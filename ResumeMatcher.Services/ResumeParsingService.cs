using ResumeMatcher.Core.Interfaces;
using ResumeMatcher.Core.Models;
using System.Diagnostics;
using System.Text.Json;
using ResumeMatcher.NET;

namespace ResumeMatcher.Services
{
    public class ResumeParsingService : IResumeParsingService
    {
        private readonly string _pythonPath = "python";
        private readonly string _scriptsDir;
        private readonly bool _useDotNet;
        private readonly ResumeParser _dotNetParser;

        public ResumeParsingService(bool useDotNet = false)
        {
            _useDotNet = useDotNet;
            Console.WriteLine($"[ResumeParsingService] Constructor called with useDotNet: {useDotNet}");
            var currentDir = Directory.GetCurrentDirectory();
            var solutionRoot = Path.GetFullPath(Path.Combine(currentDir, ".."));
            _scriptsDir = Path.Combine(solutionRoot, "PythonScripts");
            _dotNetParser = new ResumeParser();
            
            Console.WriteLine($"[ResumeParsingService] Current directory: {currentDir}");
            Console.WriteLine($"[ResumeParsingService] Solution root: {solutionRoot}");
            Console.WriteLine($"[ResumeParsingService] Scripts directory: {_scriptsDir}");
            Console.WriteLine($"[ResumeParsingService] Scripts directory exists: {Directory.Exists(_scriptsDir)}");
        }

        public async Task<string> ExtractTextFromResumeAsync(Resume resume)
        {
            if (_useDotNet)
                return _dotNetParser.ExtractTextFromResume(resume);
            var args = $"resume_parser.py extract_text {resume.FilePath}";
            var output = await RunPythonScriptAsync(args);
            return output;
        }

        public async Task<List<string>> ExtractKeywordsFromJobDescriptionAsync(string jobDescription)
        {
            if (_useDotNet)
                return _dotNetParser.ExtractKeywordsFromJobDescription(jobDescription);
            var args = $"resume_parser.py extract_keywords \"{jobDescription.Replace("\"", "'")}\"";
            var output = await RunPythonScriptAsync(args);
            return ParseJsonSafely<List<string>>(output) ?? new List<string>();
        }

        public async Task<ResumeRanking> RankResumeAsync(Resume resume, string jobDescription, List<string> requiredSkills, List<string> preferredSkills)
        {
            var swTotal = Stopwatch.StartNew();
            Console.WriteLine($"[RankResumeAsync] Ranking resume: {resume.FileName}");
            
            if (_useDotNet)
            {
                var extractedText = await ExtractTextFromResumeAsync(resume);
                if (string.IsNullOrEmpty(extractedText))
                {
                    Console.WriteLine($"[RankResumeAsync] No text extracted from {resume.FileName}");
                    return new ResumeRanking { Resume = resume, Score = 0 };
                }

                // Extract name and phone from the content
                var extractedName = _dotNetParser.ExtractName(extractedText);
                var extractedPhone = _dotNetParser.ExtractPhoneNumber(extractedText);
                
                // Update resume with extracted information
                if (!string.IsNullOrEmpty(extractedName))
                {
                    resume.Name = extractedName;
                    Console.WriteLine($"[RankResumeAsync] Extracted name: {extractedName} for {resume.FileName}");
                }
                
                if (!string.IsNullOrEmpty(extractedPhone))
                {
                    resume.Phone = extractedPhone;
                    Console.WriteLine($"[RankResumeAsync] Extracted phone: {extractedPhone} for {resume.FileName}");
                }

                var swKeywords = Stopwatch.StartNew();
                
                // Calculate matches for required skills (higher weight)
                var requiredMatches = _dotNetParser.CalculateKeywordMatches(extractedText, requiredSkills);
                foreach (var match in requiredMatches)
                {
                    match.Weight = 0.3; // Higher weight for required skills
                }
                
                // Calculate matches for preferred skills (lower weight)
                var preferredMatches = _dotNetParser.CalculateKeywordMatches(extractedText, preferredSkills);
                foreach (var match in preferredMatches)
                {
                    match.Weight = 0.1; // Lower weight for preferred skills
                }
                
                // Combine all matches
                var allMatches = new List<KeywordMatch>();
                allMatches.AddRange(requiredMatches);
                allMatches.AddRange(preferredMatches);
                
                swKeywords.Stop();
                Console.WriteLine($"[RankResumeAsync] Keyword matching took: {swKeywords.ElapsedMilliseconds} ms");
                Console.WriteLine($"[RankResumeAsync] Required skills matched: {requiredMatches.Count}, Preferred skills matched: {preferredMatches.Count}");

                var score = allMatches.Sum(km => km.Weight);
                Console.WriteLine($"[RankResumeAsync] Calculated score: {score}");

                var summary = GenerateSummary(extractedText, allMatches, score, requiredSkills, preferredSkills);

                swTotal.Stop();
                Console.WriteLine($"[RankResumeAsync] Total time for {resume.FileName}: {swTotal.ElapsedMilliseconds} ms");

                return new ResumeRanking
                {
                    Resume = resume,
                    Score = score,
                    KeywordMatches = allMatches,
                    Summary = summary,
                    ResumeSource = resume.Source ?? "Unknown"
                };
            }
            
            var args = $"resume_parser.py rank_resume '{JsonSerializer.Serialize(resume)}' \"{jobDescription.Replace("\"", "'")}\"";
            var output = await RunPythonScriptAsync(args);
            return ParseJsonSafely<ResumeRanking>(output) ?? new ResumeRanking { Resume = resume };
        }

        private string GenerateSummary(string text, List<KeywordMatch> keywordMatches, double score, List<string> requiredSkills, List<string> preferredSkills)
        {
            if (string.IsNullOrEmpty(text))
                return "No content available for summary.";

            var matchedKeywords = keywordMatches.Where(km => km.Weight > 0).Select(km => km.Keyword).ToList();
            
            if (!matchedKeywords.Any())
                return "No relevant skills or experience found.";

            var requiredMatches = matchedKeywords.Where(k => requiredSkills.Contains(k)).ToList();
            var preferredMatches = matchedKeywords.Where(k => preferredSkills.Contains(k)).ToList();
            
            var summary = $"Candidate shows proficiency in: {string.Join(", ", matchedKeywords)}. ";
            summary += $"Overall match score: {score:F2}. ";
            
            if (requiredMatches.Any())
            {
                summary += $"Required skills matched: {string.Join(", ", requiredMatches)}. ";
            }
            
            if (preferredMatches.Any())
            {
                summary += $"Preferred skills matched: {string.Join(", ", preferredMatches)}. ";
            }

            return summary;
        }

        public async Task<List<ResumeRanking>> RankResumesAsync(List<Resume> resumes, ResumeMatchingRequest request)
        {
            var swTotal = Stopwatch.StartNew();
            Console.WriteLine($"[RankResumesAsync] Starting to rank {resumes.Count} resumes");
            
            if (_useDotNet)
            {
                Console.WriteLine($"[RankResumesAsync] Using .NET logic for ranking");
                var rankings = new List<ResumeRanking>();
                
                // Separate required and preferred skills for different weighting
                var requiredSkills = request.RequiredSkills ?? new List<string>();
                var preferredSkills = request.PreferredSkills ?? new List<string>();
                
                Console.WriteLine($"[RankResumesAsync] Using {requiredSkills.Count} required skills and {preferredSkills.Count} preferred skills");
                
                var swProcessing = Stopwatch.StartNew();
                var processedCount = 0;
                
                foreach (var resume in resumes)
                {
                    var swResume = Stopwatch.StartNew();
                    var ranking = await RankResumeAsync(resume, request.JobDescription, requiredSkills, preferredSkills);
                    rankings.Add(ranking);
                    swResume.Stop();
                    processedCount++;
                    
                    if (processedCount % 5 == 0 || processedCount == resumes.Count)
                    {
                        Console.WriteLine($"[RankResumesAsync] Processed {processedCount}/{resumes.Count} resumes. Last resume took: {swResume.ElapsedMilliseconds} ms");
                    }
                }
                swProcessing.Stop();
                Console.WriteLine($"[RankResumesAsync] Processing all resumes took: {swProcessing.ElapsedMilliseconds} ms");
                
                var swSort = Stopwatch.StartNew();
                var sortedRankings = rankings
                    .OrderByDescending(r => r.Score)
                    .ThenBy(r => r.Resume?.EmailSender ?? "")
                    .ToList();
                swSort.Stop();
                Console.WriteLine($"[RankResumesAsync] Sorting took: {swSort.ElapsedMilliseconds} ms");
                
                for (int i = 0; i < sortedRankings.Count; i++)
                {
                    sortedRankings[i].Rank = i + 1;
                }
                
                swTotal.Stop();
                Console.WriteLine($"[RankResumesAsync] Total time for ranking all resumes: {swTotal.ElapsedMilliseconds} ms");
                return sortedRankings;
            }
            
            Console.WriteLine($"[RankResumesAsync] Using Python logic for ranking");
            var args = $"resume_parser.py rank_resumes '{JsonSerializer.Serialize(resumes)}' \"{request.JobDescription.Replace("\"", "'")}\"";
            var output = await RunPythonScriptAsync(args);
            var pythonRankings = ParseJsonSafely<List<ResumeRanking>>(output) ?? new List<ResumeRanking>();
            
            // Ensure Python rankings also have proper ranks assigned
            var sortedPythonRankings = pythonRankings
                .OrderByDescending(r => r.Score)
                .ThenBy(r => r.Resume?.EmailSender ?? "")
                .ToList();
            
            for (int i = 0; i < sortedPythonRankings.Count; i++)
            {
                sortedPythonRankings[i].Rank = i + 1;
            }
            
            swTotal.Stop();
            Console.WriteLine($"[RankResumesAsync] Total time for ranking all resumes: {swTotal.ElapsedMilliseconds} ms");
            return sortedPythonRankings;
        }

        private async Task<string> RunPythonScriptAsync(string scriptArgs)
        {
            try
            {
                var scriptPath = Path.Combine(_scriptsDir, scriptArgs);
                Console.WriteLine($"Running Python script: {scriptPath}");
                
                var psi = new ProcessStartInfo
                {
                    FileName = _pythonPath,
                    Arguments = scriptPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = _scriptsDir
                };
                
                using var process = Process.Start(psi);
                if (process == null)
                    return string.Empty;
                    
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"Python script error: {error}");
                }
                
                return output?.Trim() ?? string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running Python script: {ex.Message}");
                return string.Empty;
            }
        }

        private T? ParseJsonSafely<T>(string json)
        {
            Console.WriteLine($"[ResumeParsingService] Raw JSON from Python: {json}");
            if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine("[ResumeParsingService] JSON is empty or whitespace.");
                return default(T);
            }
            try
            {
                var result = JsonSerializer.Deserialize<T>(json);
                if (result is List<ResumeRanking> rankings)
                {
                    Console.WriteLine($"[ResumeParsingService] Parsed {rankings.Count} rankings from JSON.");
                }
                return result;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[ResumeParsingService] JSON parsing error: {ex.Message}");
                Console.WriteLine($"[ResumeParsingService] JSON content: {json}");
                return default(T);
            }
        }
    }
} 