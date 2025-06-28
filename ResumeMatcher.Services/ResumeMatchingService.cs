using ResumeMatcher.Core.Interfaces;
using ResumeMatcher.Core.Models;

namespace ResumeMatcher.Services
{
    public class ResumeMatchingService : IResumeMatchingService
    {
        private readonly IResumeSourcingService _sourcingService;
        private readonly IResumeParsingService _parsingService;
        private readonly IExternalNotificationService _notificationService;

        public ResumeMatchingService(
            IResumeSourcingService sourcingService, 
            IResumeParsingService parsingService,
            IExternalNotificationService notificationService)
        {
            _sourcingService = sourcingService;
            _parsingService = parsingService;
            _notificationService = notificationService;
        }

        public async Task<ResumeMatchingResponse> MatchResumesAsync(ResumeMatchingRequest request)
        {
            var resumes = await _sourcingService.GetAllResumesAsync(request);
            var rankings = await _parsingService.RankResumesAsync(resumes, request);
            
            // Convert ResumeRanking to ResumeRankingSummary to exclude content from response
            var summaryRankings = rankings.Select(r => new ResumeRankingSummary
            {
                Resume = new ResumeSummary
                {
                    Id = r.Resume.Id,
                    FileName = r.Resume.FileName,
                    FilePath = r.Resume.FilePath,
                    Content = r.Resume.Content,
                    EmailSubject = r.Resume.EmailSubject,
                    EmailSender = r.Resume.EmailSender,
                    Email = r.Resume.Email,
                    Phone = r.Resume.Phone,
                    Name = r.Resume.Name,
                    EmailDate = r.Resume.EmailDate,
                    Source = r.Resume.Source,
                    CreatedAt = r.Resume.CreatedAt,
                    ProcessedAt = r.Resume.ProcessedAt,
                    Status = r.Resume.Status
                },
                Score = r.Score,
                Rank = r.Rank,
                KeywordMatches = r.KeywordMatches,
                Summary = r.Summary,
                ResumeSource = r.ResumeSource
            }).ToList();
            
            // Filter out unmatched resumes (score = 0) if excludeUnmatched is true
            if (request.ExcludeUnmatched)
            {
                summaryRankings = summaryRankings.Where(r => r.Score > 0).ToList();
            }
            
            var response = new ResumeMatchingResponse
            {
                Rankings = summaryRankings,
                TotalResumesProcessed = resumes.Count,
                JobDescription = request.JobDescription,
                JobTitle = request.JobTitle,
                PromptId = request.PromptId,
                ExtractedKeywords = await _parsingService.ExtractKeywordsFromJobDescriptionAsync(request.JobDescription)
            };

            // Fire and forget notification with top 2 resumes
            var topResumes = summaryRankings.Take(2).Select(r => r.Resume).ToList();
            if (request.EnableExternalNotification)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.SendResumeNotificationAsync(topResumes, request.JobDescription, request.JobTitle, request.PromptId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ResumeMatchingService] Error in notification task: {ex.Message}");
                    }
                });
            }
            else
            {
                Console.WriteLine("[ResumeMatchingService] External notification is disabled by request payload.");
            }

            return response;
        }

        public async Task<List<Resume>> GetAvailableResumesAsync()
        {
            return await _sourcingService.GetAllResumesAsync(new ResumeMatchingRequest());
        }

        public async Task<ResumeRanking> GetResumeRankingAsync(string resumeId, string jobDescription, bool useOllama = false)
        {
            var resumes = await _sourcingService.GetAllResumesAsync(new ResumeMatchingRequest());
            var resume = resumes.FirstOrDefault(r => r.Id == resumeId);
            if (resume == null) return new ResumeRanking();
            
            var keywords = await _parsingService.ExtractKeywordsFromJobDescriptionAsync(jobDescription);
            
            if (useOllama)
            {
                // Note: This would need the Ollama service to be implemented
                return await _parsingService.RankResumeAsync(resume, jobDescription, keywords, new List<string>());
            }
            else
            {
                return await _parsingService.RankResumeAsync(resume, jobDescription, keywords, new List<string>());
            }
        }
    }
} 