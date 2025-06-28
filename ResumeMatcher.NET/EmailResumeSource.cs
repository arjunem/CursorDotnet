using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit;
using MimeKit;
using ResumeMatcher.Core.Models;
using UglyToad.PdfPig;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ResumeMatcher.NET
{
    public class EmailResumeSource
    {
        private readonly string _imapServer;
        private readonly int _imapPort;
        private readonly string _email;
        private readonly string _password;
        private readonly string _tempDir;
        private readonly ResumeParser _resumeParser;
        private readonly Dictionary<string, DateTime> _lastFetchCache = new();
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(5); // Cache for 5 minutes

        public EmailResumeSource(string imapServer, int imapPort, string email, string password, string tempDir = "temp_resumes")
        {
            _imapServer = imapServer;
            _imapPort = imapPort;
            _email = email;
            _password = password;
            _tempDir = tempDir;
            _resumeParser = new ResumeParser();
            if (!Directory.Exists(_tempDir))
                Directory.CreateDirectory(_tempDir);
        }

        // Helper function to extract email from content
        private string ExtractEmailFromContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return null;
            
            // Regex pattern to find email addresses
            var emailPattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b";
            var match = Regex.Match(content, emailPattern);
            return match.Success ? match.Value : null;
        }

        // Helper function to extract phone number from content
        private string ExtractPhoneFromContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return null;
            
            return _resumeParser.ExtractPhoneNumber(content);
        }

        public async Task<List<Resume>> FetchResumesFromEmailAsync(string subjectFilter = "resume", List<string> attachmentExtensions = null, int maxEmails = 50, bool unreadOnly = false)
        {
            var cacheKey = $"email_{subjectFilter}_{unreadOnly}_{DateTime.UtcNow:yyyyMMdd_HH}";
            
            // Check cache first
            if (_lastFetchCache.TryGetValue(cacheKey, out var lastFetch) && 
                DateTime.UtcNow - lastFetch < _cacheTimeout)
            {
                Console.WriteLine($"[EmailResumeSource] Using cached results from {lastFetch}");
                return new List<Resume>(); // Return empty list for cached results
            }

            Console.WriteLine($"[EmailResumeSource] Starting email fetch at {DateTime.UtcNow} (Unread only: {unreadOnly})");
            var totalStartTime = DateTime.UtcNow;
            
            var resumes = new List<Resume>();
            attachmentExtensions ??= new List<string> { ".pdf", ".docx", ".doc" };
            
            try
            {
                using var client = new ImapClient();
                
                // Set timeout for faster failure detection
                client.Timeout = 60000; // 60 seconds timeout
                
                // Phase 1: Connection
                var connectionStart = DateTime.UtcNow;
                Console.WriteLine($"[EmailResumeSource] Phase 1: Connecting to {_imapServer}:{_imapPort}");
                await client.ConnectAsync(_imapServer, _imapPort, true);
                var connectionTime = DateTime.UtcNow - connectionStart;
                Console.WriteLine($"[EmailResumeSource] Connection took: {connectionTime.TotalMilliseconds:F0} ms");
                
                // Phase 2: Authentication
                var authStart = DateTime.UtcNow;
                Console.WriteLine($"[EmailResumeSource] Phase 2: Authenticating...");
                await client.AuthenticateAsync(_email, _password);
                var authTime = DateTime.UtcNow - authStart;
                Console.WriteLine($"[EmailResumeSource] Authentication took: {authTime.TotalMilliseconds:F0} ms");
                
                // Phase 3: Open inbox
                var inboxStart = DateTime.UtcNow;
                var inbox = client.Inbox;
                await inbox.OpenAsync(FolderAccess.ReadOnly);
                var inboxTime = DateTime.UtcNow - inboxStart;
                Console.WriteLine($"[EmailResumeSource] Opening inbox took: {inboxTime.TotalMilliseconds:F0} ms");
                
                // Phase 4: Search for emails
                var searchStart = DateTime.UtcNow;
                Console.WriteLine($"[EmailResumeSource] Phase 4: Searching for emails with subject containing '{subjectFilter}' (Unread only: {unreadOnly})");
                
                // Build search query
                SearchQuery query = SearchQuery.SubjectContains(subjectFilter);
                if (unreadOnly)
                {
                    query = SearchQuery.And(query, SearchQuery.NotSeen);
                    Console.WriteLine($"[EmailResumeSource] Added unread filter to search query");
                }
                
                var uids = await inbox.SearchAsync(query);
                var searchTime = DateTime.UtcNow - searchStart;
                Console.WriteLine($"[EmailResumeSource] Search took: {searchTime.TotalMilliseconds:F0} ms");
                Console.WriteLine($"[EmailResumeSource] Found {uids.Count} emails to process");
                
                // Apply max emails limit if specified
                var emailsToProcess = maxEmails > 0 ? uids.TakeLast(maxEmails).ToList() : uids.ToList();
                Console.WriteLine($"[EmailResumeSource] Processing {emailsToProcess.Count} emails (max limit: {maxEmails})");
                
                // Phase 5: Process emails and attachments
                var processingStart = DateTime.UtcNow;
                var emailCount = 0;
                var attachmentCount = 0;
                var processedCount = 0;
                
                foreach (var uid in emailsToProcess)
                {
                    try
                    {
                        var messageStart = DateTime.UtcNow;
                        var message = await inbox.GetMessageAsync(uid);
                        var messageTime = DateTime.UtcNow - messageStart;
                        
                        var attachments = message.Attachments.Count();
                        if (attachments == 0) continue;
                        
                        emailCount++;
                        attachmentCount += attachments;
                        
                        Console.WriteLine($"[EmailResumeSource] Message {uid}: {attachments} attachments, fetch took: {messageTime.TotalMilliseconds:F0} ms");
                        
                        foreach (var attachment in message.Attachments)
                        {
                            if (attachment is MimePart part)
                            {
                                var fileName = part.FileName;
                                if (attachmentExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                                {
                                    // Extract text content directly from the attachment stream
                                    var extractStart = DateTime.UtcNow;
                                    var resumeContent = await ExtractTextFromAttachmentAsync(part, fileName);
                                    var extractTime = DateTime.UtcNow - extractStart;
                                    processedCount++;
                                    
                                    Console.WriteLine($"[EmailResumeSource] Text extraction for {fileName} took: {extractTime.TotalMilliseconds:F0} ms");
                                    
                                    // Extract email from the resume content
                                    var extractedEmail = ExtractEmailFromContent(resumeContent);
                                    
                                    // If no email found in content, fall back to sender email
                                    if (string.IsNullOrEmpty(extractedEmail))
                                    {
                                        extractedEmail = ExtractEmailFromSender(message.From.ToString());
                                    }
                                    
                                    // Extract phone number from the resume content
                                    var extractedPhone = ExtractPhoneFromContent(resumeContent);
                                    
                                    resumes.Add(new Resume
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        FileName = fileName,
                                        FilePath = $"/email/{fileName}", // Virtual path since we don't save the file
                                        Content = resumeContent, // Store the extracted content
                                        EmailSubject = message.Subject,
                                        EmailSender = message.From.ToString(),
                                        Email = extractedEmail, // Email from resume content or sender
                                        Phone = extractedPhone, // Phone number from resume content
                                        EmailDate = message.Date.DateTime,
                                        Source = "Email",
                                        CreatedAt = DateTime.UtcNow,
                                        Status = ResumeStatus.Pending
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[EmailResumeSource] Error processing message {uid}: {ex.Message}");
                        continue; // Continue with next message
                    }
                }
                
                var processingTime = DateTime.UtcNow - processingStart;
                Console.WriteLine($"[EmailResumeSource] Processing phase took: {processingTime.TotalMilliseconds:F0} ms");
                Console.WriteLine($"[EmailResumeSource] Processed {emailCount} emails, {attachmentCount} attachments, {processedCount} resume files");
                
                // Phase 6: Disconnect
                var disconnectStart = DateTime.UtcNow;
                await client.DisconnectAsync(true);
                var disconnectTime = DateTime.UtcNow - disconnectStart;
                Console.WriteLine($"[EmailResumeSource] Disconnect took: {disconnectTime.TotalMilliseconds:F0} ms");
                
                // Update cache
                _lastFetchCache[cacheKey] = DateTime.UtcNow;
                
                var totalTime = DateTime.UtcNow - totalStartTime;
                Console.WriteLine($"[EmailResumeSource] === TIMING BREAKDOWN ===");
                Console.WriteLine($"[EmailResumeSource] Connection: {connectionTime.TotalMilliseconds:F0} ms");
                Console.WriteLine($"[EmailResumeSource] Authentication: {authTime.TotalMilliseconds:F0} ms");
                Console.WriteLine($"[EmailResumeSource] Open Inbox: {inboxTime.TotalMilliseconds:F0} ms");
                Console.WriteLine($"[EmailResumeSource] Search: {searchTime.TotalMilliseconds:F0} ms");
                Console.WriteLine($"[EmailResumeSource] Processing: {processingTime.TotalMilliseconds:F0} ms");
                Console.WriteLine($"[EmailResumeSource] Disconnect: {disconnectTime.TotalMilliseconds:F0} ms");
                Console.WriteLine($"[EmailResumeSource] Total time: {totalTime.TotalMilliseconds:F0} ms");
                Console.WriteLine($"[EmailResumeSource] Found {resumes.Count} resumes.");
                
                return resumes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailResumeSource] Error during email fetch: {ex.Message}");
                return new List<Resume>();
            }
        }

        private async Task<string> ExtractTextFromAttachmentAsync(MimePart part, string fileName)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                await part.Content.DecodeToAsync(memoryStream);
                memoryStream.Position = 0;

                if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    return ExtractTextFromPdfStream(memoryStream, fileName);
                }
                else if (fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                {
                    return ExtractTextFromDocxStream(memoryStream, fileName);
                }
                else if (fileName.EndsWith(".doc", StringComparison.OrdinalIgnoreCase))
                {
                    return ExtractTextFromDocStream(memoryStream, fileName);
                }
                else
                {
                    return $"Unsupported file type: {fileName}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailResumeSource] Error extracting text from {fileName}: {ex.Message}");
                return $"Error extracting content from {fileName}: {ex.Message}";
            }
        }

        private string ExtractTextFromPdfStream(MemoryStream stream, string fileName)
        {
            try
            {
                using var pdf = PdfDocument.Open(stream);
                return string.Join("\n", pdf.GetPages().Select(p => p.Text));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailResumeSource] PDF parsing failed for {fileName}: {ex.Message}");
                return $"PDF file: {fileName} - Content could not be extracted due to parsing error.";
            }
        }

        private string ExtractTextFromDocxStream(MemoryStream stream, string fileName)
        {
            try
            {
                using var doc = WordprocessingDocument.Open(stream, false);
                var body = doc.MainDocumentPart.Document.Body;
                return body.InnerText;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailResumeSource] DOCX parsing failed for {fileName}: {ex.Message}");
                return $"DOCX file: {fileName} - Content could not be extracted due to parsing error.";
            }
        }

        private string ExtractTextFromDocStream(MemoryStream stream, string fileName)
        {
            // For .doc files, we'll return a placeholder since OpenXML doesn't support .doc format
            // In a production environment, you might want to use a library like Aspose.Words or convert .doc to .docx
            return $"DOC file: {fileName} - .doc format not supported in this implementation.";
        }

        // Helper function to extract email from sender string
        private string ExtractEmailFromSender(string sender)
        {
            if (string.IsNullOrEmpty(sender))
                return null;
            
            // Extract email from format like "John Doe <john.doe@example.com>"
            var emailPattern = @"<([^>]+)>";
            var match = Regex.Match(sender, emailPattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            
            // If no angle brackets, try to find email pattern in the string
            var emailOnlyPattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b";
            var emailMatch = Regex.Match(sender, emailOnlyPattern);
            return emailMatch.Success ? emailMatch.Value : null;
        }
    }
} 