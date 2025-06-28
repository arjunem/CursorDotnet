using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ResumeMatcher.Core.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ResumeMatcher.NET
{
    public class ResumeParser
    {
        public string ExtractTextFromFile(string filePath)
        {
            try
            {
                if (filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    return ExtractTextFromPdf(filePath);
                if (filePath.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                    return ExtractTextFromDocx(filePath);
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ResumeParser] File parsing failed for {filePath}: {ex.Message}");
                return $"File: {Path.GetFileName(filePath)} - Content could not be extracted due to error.";
            }
        }

        public string ExtractTextFromStream(Stream stream, string fileName)
        {
            try
            {
                if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    return ExtractTextFromPdfStream(stream, fileName);
                if (fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                    return ExtractTextFromDocxStream(stream, fileName);
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ResumeParser] Stream parsing failed for {fileName}: {ex.Message}");
                return $"File: {fileName} - Content could not be extracted due to error.";
            }
        }

        public string ExtractTextFromPdf(string filePath)
        {
            try
            {
                using var pdf = PdfDocument.Open(filePath);
                return string.Join("\n", pdf.GetPages().Select(p => p.Text));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ResumeParser] PDF parsing failed for {filePath}: {ex.Message}");
                // Return a fallback text for PDFs that can't be parsed
                return $"PDF file: {Path.GetFileName(filePath)} - Content could not be extracted due to parsing error.";
            }
        }

        public string ExtractTextFromPdfStream(Stream stream, string fileName)
        {
            try
            {
                using var pdf = PdfDocument.Open(stream);
                return string.Join("\n", pdf.GetPages().Select(p => p.Text));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ResumeParser] PDF stream parsing failed for {fileName}: {ex.Message}");
                return $"PDF file: {fileName} - Content could not be extracted due to parsing error.";
            }
        }

        public string ExtractTextFromDocx(string filePath)
        {
            try
            {
                using var doc = WordprocessingDocument.Open(filePath, false);
                var body = doc.MainDocumentPart.Document.Body;
                return body.InnerText;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ResumeParser] DOCX parsing failed for {filePath}: {ex.Message}");
                // Return a fallback text for DOCX files that can't be parsed
                return $"DOCX file: {Path.GetFileName(filePath)} - Content could not be extracted due to parsing error.";
            }
        }

        public string ExtractTextFromDocxStream(Stream stream, string fileName)
        {
            try
            {
                using var doc = WordprocessingDocument.Open(stream, false);
                var body = doc.MainDocumentPart.Document.Body;
                return body.InnerText;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ResumeParser] DOCX stream parsing failed for {fileName}: {ex.Message}");
                return $"DOCX file: {fileName} - Content could not be extracted due to parsing error.";
            }
        }

        public string ExtractTextFromResume(Resume resume)
        {
            // If the resume has content stored (from database), use that
            if (!string.IsNullOrEmpty(resume.Content))
            {
                Console.WriteLine($"[ResumeParser] Using stored content for {resume.FileName}");
                return resume.Content;
            }
            
            // Check if file exists
            if (!File.Exists(resume.FilePath))
            {
                // For database resumes that don't have actual files, return a fallback text
                Console.WriteLine($"[ResumeParser] File not found: {resume.FilePath}, using fallback text");
                return $"Resume: {resume.FileName} - {resume.EmailSender} - {resume.EmailSubject}";
            }
            
            // Otherwise try to extract from file
            return ExtractTextFromFile(resume.FilePath);
        }

        public List<string> ExtractKeywordsFromJobDescription(string jobDescription, int maxKeywords = 20)
        {
            var tokens = Regex.Split(jobDescription.ToLower(), @"\W+")
                .Where(t => t.Length > 2 && !StopWords.Contains(t))
                .ToList();
            return tokens.GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .Take(maxKeywords)
                .Select(g => g.Key)
                .ToList();
        }

        public List<KeywordMatch> CalculateKeywordMatches(string resumeText, List<string> keywords)
        {
            var matches = new List<KeywordMatch>();
            var resumeLower = resumeText.ToLower();
            foreach (var keyword in keywords)
            {
                var pattern = "\\b" + Regex.Escape(keyword.ToLower()) + "\\b";
                var count = Regex.Matches(resumeLower, pattern).Count;
                if (count > 0)
                {
                    matches.Add(new KeywordMatch
                    {
                        Keyword = keyword,
                        // Count = count,
                        Weight = 0.1, // Fixed weight of 0.1 per keyword, regardless of frequency
                        // Context = FindKeywordContext(resumeText, keyword)
                    });
                }
            }
            return matches;
        }

        // public List<string> FindKeywordContext(string text, string keyword, int contextLength = 50)
        // {
        //     var contexts = new List<string>();
        //     var regex = new Regex($"(.{{0,{contextLength}}}{Regex.Escape(keyword)}.{{0,{contextLength}}})", RegexOptions.IgnoreCase);
        //     foreach (Match match in regex.Matches(text))
        //     {
        //         contexts.Add(match.Value.Trim());
        //         if (contexts.Count >= 3) break;
        //     }
        //     return contexts;
        // }

        // public double CalculateSkillsMatchPercentage(string resumeText, List<string> requiredSkills)
        // {
        //     if (requiredSkills == null || requiredSkills.Count == 0) return 0.0;
        //     var resumeLower = resumeText.ToLower();
        //     var found = requiredSkills.Count(skill => resumeLower.Contains(skill.ToLower()));
        //     return (double)found / requiredSkills.Count * 100.0;
        // }

        public string? ExtractPhoneNumber(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            // Common phone number patterns including Indian numbers
            var patterns = new[]
            {
                // Indian mobile numbers: +91 98765 43210, +91-98765-43210, 98765 43210, 9876543210
                @"(?:\+91[\s-]?)?[6-9]\d{9}",
                // Indian landline: +91 11 2345 6789, 011-2345-6789, 011 2345 6789
                @"(?:\+91[\s-]?)?(?:0)?[1-9]\d{1,4}[\s-]?\d{3,4}[\s-]?\d{4}",
                // US patterns: (555) 123-4567, 555-123-4567, 555.123.4567
                @"\(\d{3}\)\s*\d{3}-\d{4}",
                @"\d{3}-\d{3}-\d{4}",
                @"\d{3}\.\d{3}\.\d{4}",
                @"\d{3}\s\d{3}\s\d{4}",
                @"\b\d{10}\b",
                // International: +1 555 123 4567
                @"\+\d{1,3}\s*\d{3}\s*\d{3}\s*\d{4}",
                @"1-\d{3}-\d{3}-\d{4}"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern);
                if (match.Success)
                {
                    return match.Value.Trim();
                }
            }

            return null;
        }

        public string? ExtractName(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            Console.WriteLine($"[ResumeParser] Extracting name from text (first 200 chars): {text.Substring(0, Math.Min(200, text.Length))}...");

            // Split text into lines and look for name patterns
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            Console.WriteLine($"[ResumeParser] Checking first {Math.Min(10, lines.Length)} lines for name patterns");
            
            foreach (var line in lines.Take(10)) // Check first 10 lines
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine)) continue;

                Console.WriteLine($"[ResumeParser] Checking line: '{trimmedLine}'");

                // Pattern 1: Full name at the beginning of resume (usually first line)
                // Matches: "John Doe", "John A. Doe", "John A Doe", "JOHN DOE"
                var namePattern = @"^([A-Z][a-z]+(?:\s+[A-Z][a-z]+)*)$";
                var match = Regex.Match(trimmedLine, namePattern);
                if (match.Success)
                {
                    var name = match.Groups[1].Value.Trim();
                    if (name.Split(' ').Length >= 2 && name.Split(' ').Length <= 4)
                    {
                        Console.WriteLine($"[ResumeParser] Found name (Pattern 1): {name}");
                        return name;
                    }
                }

                // Pattern 2: Name with title (Mr., Ms., Dr., etc.)
                var nameWithTitlePattern = @"^(Mr\.|Ms\.|Mrs\.|Dr\.|Prof\.)\s+([A-Z][a-z]+(?:\s+[A-Z][a-z]+)*)$";
                match = Regex.Match(trimmedLine, nameWithTitlePattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var name = match.Groups[2].Value.Trim();
                    if (name.Split(' ').Length >= 2 && name.Split(' ').Length <= 4)
                    {
                        Console.WriteLine($"[ResumeParser] Found name (Pattern 2): {name}");
                        return name;
                    }
                }

                // Pattern 3: Name in ALL CAPS (common in resumes)
                var allCapsPattern = @"^([A-Z]+(?:\s+[A-Z]+)*)$";
                match = Regex.Match(trimmedLine, allCapsPattern);
                if (match.Success)
                {
                    var name = match.Groups[1].Value.Trim();
                    if (name.Split(' ').Length >= 2 && name.Split(' ').Length <= 4)
                    {
                        // Convert to proper case
                        var properName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
                        Console.WriteLine($"[ResumeParser] Found name (Pattern 3): {properName}");
                        return properName;
                    }
                }

                // Pattern 4: More flexible name pattern - allows for single names or names with middle initials
                var flexibleNamePattern = @"^([A-Z][a-z]+(?:\s+[A-Z]?[a-z]*)*)$";
                match = Regex.Match(trimmedLine, flexibleNamePattern);
                if (match.Success)
                {
                    var name = match.Groups[1].Value.Trim();
                    var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length >= 1 && words.Length <= 4)
                    {
                        // Check if it looks like a name (not a job title, company, etc.)
                        var commonJobWords = new[] { "engineer", "developer", "manager", "director", "president", "ceo", "cto", "consultant", "analyst", "specialist", "coordinator", "assistant", "associate", "senior", "junior", "lead", "principal", "architect", "designer", "programmer", "coder" };
                        var isLikelyName = !commonJobWords.Any(word => name.ToLower().Contains(word));
                        
                        if (isLikelyName)
                        {
                            Console.WriteLine($"[ResumeParser] Found name (Pattern 4): {name}");
                            return name;
                        }
                    }
                }

                // Pattern 5: Name followed by common resume headers
                var nameWithHeaderPattern = @"^([A-Z][a-z]+(?:\s+[A-Z][a-z]+)*)\s*(?:resume|cv|curriculum vitae|profile|contact|email|phone|address|summary|objective|experience|education|skills)$";
                match = Regex.Match(trimmedLine, nameWithHeaderPattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var name = match.Groups[1].Value.Trim();
                    if (name.Split(' ').Length >= 2 && name.Split(' ').Length <= 4)
                    {
                        Console.WriteLine($"[ResumeParser] Found name (Pattern 5): {name}");
                        return name;
                    }
                }
            }

            Console.WriteLine($"[ResumeParser] No name found in the first 10 lines");
            return null;
        }

        // Add more methods as needed for experience, education, and ranking

        private static readonly HashSet<string> StopWords = new HashSet<string>(new[]
        {
            "the", "and", "for", "with", "that", "have", "this", "from", "are", "was", "but", "not", "all", "can", "has", "will", "one", "their", "about", "which", "when", "make", "like", "time", "just", "know", "take", "into", "your", "some", "them", "other", "than", "then", "now", "look", "only", "come", "its", "over", "think", "also", "back", "after", "use", "two", "how", "our", "work", "first", "well", "way", "even", "new", "want", "because", "any", "these", "give", "most", "us", "experience", "years", "skills", "education", "work", "job", "position"
        });
    }
} 