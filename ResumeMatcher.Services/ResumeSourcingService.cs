using ResumeMatcher.Core.Interfaces;
using ResumeMatcher.Core.Models;
using System.Diagnostics;
using System.Text.Json;
using ResumeMatcher.NET;
using Microsoft.Extensions.Configuration;

namespace ResumeMatcher.Services
{
    public class ResumeSourcingService : IResumeSourcingService
    {
        private readonly string _pythonPath = "python";
        private readonly string _scriptsDir;
        private readonly bool _useDotNet;
        private readonly DatabaseResumeSource _dbSource;
        private readonly EmailResumeSource _emailSource;

        public ResumeSourcingService(bool useDotNet, IConfiguration configuration)
        {
            _useDotNet = useDotNet;
            Console.WriteLine($"[ResumeSourcingService] Constructor called with useDotNet: {useDotNet}");
            var currentDir = Directory.GetCurrentDirectory();
            var solutionRoot = Path.GetFullPath(Path.Combine(currentDir, ".."));
            _scriptsDir = Path.Combine(solutionRoot, "PythonScripts");
            var dbPath = Path.Combine(solutionRoot, "resumes.db");
            _dbSource = new DatabaseResumeSource(dbPath);
            var emailSection = configuration.GetSection("Email");
            var imapServer = emailSection.GetValue<string>("ImapServer");
            var imapPort = emailSection.GetValue<int>("ImapPort");
            var email = emailSection.GetValue<string>("Email");
            var password = emailSection.GetValue<string>("Password");
            var tempDir = emailSection.GetValue<string>("TempDir");
            _emailSource = new EmailResumeSource(
                imapServer: imapServer,
                imapPort: imapPort,
                email: email,
                password: password,
                tempDir: Path.Combine(solutionRoot, tempDir ?? "PythonScripts/temp_resumes")
            );
            
            Console.WriteLine($"[ResumeSourcingService] Current directory: {currentDir}");
            Console.WriteLine($"[ResumeSourcingService] Solution root: {solutionRoot}");
            Console.WriteLine($"[ResumeSourcingService] Scripts directory: {_scriptsDir}");
            Console.WriteLine($"[ResumeSourcingService] Scripts directory exists: {Directory.Exists(_scriptsDir)}");
        }

        public async Task<List<Resume>> GetResumesFromEmailAsync(string subjectFilter = "resume", List<string> attachmentExtensions = null)
        {
            if (_useDotNet)
                return await _emailSource.FetchResumesFromEmailAsync(subjectFilter, attachmentExtensions);
            var args = $"email_source.py";
            var output = await RunPythonScriptAsync(args);
            return ParseResumesFromJson(output);
        }

        public async Task<List<Resume>> GetResumesFromDatabaseAsync()
        {
            if (_useDotNet)
                return await _dbSource.FetchResumesFromDatabaseAsync();
            var args = $"db_source.py";
            var output = await RunPythonScriptAsync(args);
            return ParseResumesFromJson(output);
        }

        public async Task<List<Resume>> GetAllResumesAsync(ResumeMatchingRequest request)
        {
            var resumes = new List<Resume>();
            if (request.IncludeEmailResumes)
                resumes.AddRange(await GetResumesFromEmailAsync());
            if (request.IncludeDatabaseResumes)
                resumes.AddRange(await GetResumesFromDatabaseAsync());
            return resumes;
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

        private List<Resume> ParseResumesFromJson(string json)
        {
            Console.WriteLine($"[ResumeSourcingService] Raw JSON from Python: {json}");
            if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine("[ResumeSourcingService] JSON is empty or whitespace.");
                return new List<Resume>();
            }
            try
            {
                var resumes = JsonSerializer.Deserialize<List<Resume>>(json) ?? new List<Resume>();
                Console.WriteLine($"[ResumeSourcingService] Parsed {resumes.Count} resumes from JSON.");
                return resumes;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[ResumeSourcingService] JSON parsing error: {ex.Message}");
                Console.WriteLine($"[ResumeSourcingService] JSON content: {json}");
                return new List<Resume>();
            }
        }
    }
} 