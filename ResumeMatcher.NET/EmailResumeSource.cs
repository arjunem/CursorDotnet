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

        public async Task<List<Resume>> FetchResumesFromEmailAsync(string subjectFilter = "resume", List<string> attachmentExtensions = null)
        {
            var resumes = new List<Resume>();
            attachmentExtensions ??= new List<string> { ".pdf", ".docx", ".doc" };
            using var client = new ImapClient();
            await client.ConnectAsync(_imapServer, _imapPort, true);
            await client.AuthenticateAsync(_email, _password);
            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly);
            var query = SearchQuery.SubjectContains(subjectFilter);
            var uids = await inbox.SearchAsync(query);
            foreach (var uid in uids)
            {
                var message = await inbox.GetMessageAsync(uid);
                foreach (var attachment in message.Attachments)
                {
                    if (attachment is MimePart part)
                    {
                        var fileName = part.FileName;
                        if (attachmentExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                        {
                            var filePath = Path.Combine(_tempDir, fileName);
                            using (var stream = File.Create(filePath))
                                await part.Content.DecodeToAsync(stream);
                            
                            // Extract text content from the resume file
                            var resumeContent = _resumeParser.ExtractTextFromFile(filePath);
                            
                            // Extract email from the resume content
                            var extractedEmail = ExtractEmailFromContent(resumeContent);
                            
                            // If no email found in content, fall back to sender email
                            if (string.IsNullOrEmpty(extractedEmail))
                            {
                                extractedEmail = ExtractEmailFromSender(message.From.ToString());
                            }
                            
                            resumes.Add(new Resume
                            {
                                Id = Guid.NewGuid().ToString(),
                                FileName = fileName,
                                FilePath = filePath,
                                // Content = resumeContent, // Store the parsed content
                                EmailSubject = message.Subject,
                                EmailSender = message.From.ToString(),
                                Email = extractedEmail, // Email from resume content or sender
                                EmailDate = message.Date.DateTime,
                                Source = "Email",
                                CreatedAt = DateTime.UtcNow,
                                Status = ResumeStatus.Pending
                            });
                        }
                    }
                }
            }
            await client.DisconnectAsync(true);
            return resumes;
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