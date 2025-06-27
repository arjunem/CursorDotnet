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

        public async Task<ResumeRanking> RankResumeAsync(Resume resume, string jobDescription, List<string> keywords)
        {
            var swTotal = Stopwatch.StartNew();
            if (_useDotNet)
            {
                Console.WriteLine($"[RankResumeAsync] Start ranking for: {resume.FileName}");
                var swExtract = Stopwatch.StartNew();
                string extractedText = string.Empty;
                if (!string.IsNullOrWhiteSpace(resume.Content))
                {
                    extractedText = resume.Content;
                    Console.WriteLine($"[RankResumeAsync] Used stored resume.Content for {resume.FileName}");
                }
                else
                {
                    extractedText = await ExtractTextFromResumeAsync(resume);
                    Console.WriteLine($"[RankResumeAsync] Extracted text from file for {resume.FileName}");
                }
                swExtract.Stop();
                Console.WriteLine($"[RankResumeAsync] Text extraction took: {swExtract.ElapsedMilliseconds} ms");

                var swKeywords = Stopwatch.StartNew();
                var keywordMatches = _dotNetParser.CalculateKeywordMatches(extractedText, keywords);
                swKeywords.Stop();
                Console.WriteLine($"[RankResumeAsync] Keyword matching took: {swKeywords.ElapsedMilliseconds} ms");

                var score = keywordMatches.Sum(km => km.Weight);
                Console.WriteLine($"[RankResumeAsync] Calculated score: {score}");

                swTotal.Stop();
                Console.WriteLine($"[RankResumeAsync] Total time for {resume.FileName}: {swTotal.ElapsedMilliseconds} ms");

                return new ResumeRanking
                {
                    Resume = resume,
                    Score = score,
                    KeywordMatches = keywordMatches,
                    ResumeSource = resume.Source ?? "Unknown"
                };
            }
            
            var args = $"resume_parser.py rank_resume '{JsonSerializer.Serialize(resume)}' \"{jobDescription.Replace("\"", "'")}\"";
            var output = await RunPythonScriptAsync(args);
            return ParseJsonSafely<ResumeRanking>(output) ?? new ResumeRanking { Resume = resume };
        }

        public async Task<List<ResumeRanking>> RankResumesAsync(List<Resume> resumes, ResumeMatchingRequest request)
        {
            var swTotal = Stopwatch.StartNew();
            if (_useDotNet)
            {
                Console.WriteLine($"[RankResumesAsync] Ranking {resumes.Count} resumes");
                var rankings = new List<ResumeRanking>();
                var keywords = new List<string>();
                if (request.RequiredSkills != null) keywords.AddRange(request.RequiredSkills);
                if (request.PreferredSkills != null) keywords.AddRange(request.PreferredSkills);
                foreach (var resume in resumes)
                {
                    var ranking = await RankResumeAsync(resume, request.JobDescription, keywords);
                    rankings.Add(ranking);
                }
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