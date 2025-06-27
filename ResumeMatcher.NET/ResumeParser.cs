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

        // Add more methods as needed for experience, education, and ranking

        private static readonly HashSet<string> StopWords = new HashSet<string>(new[]
        {
            "the", "and", "for", "with", "that", "have", "this", "from", "are", "was", "but", "not", "all", "can", "has", "will", "one", "their", "about", "which", "when", "make", "like", "time", "just", "know", "take", "into", "your", "some", "them", "other", "than", "then", "now", "look", "only", "come", "its", "over", "think", "also", "back", "after", "use", "two", "how", "our", "work", "first", "well", "way", "even", "new", "want", "because", "any", "these", "give", "most", "us", "experience", "years", "skills", "education", "work", "job", "position"
        });
    }
} 