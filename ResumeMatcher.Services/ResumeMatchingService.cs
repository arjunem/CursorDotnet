using ResumeMatcher.Core.Interfaces;
using ResumeMatcher.Core.Models;

namespace ResumeMatcher.Services
{
    public class ResumeMatchingService : IResumeMatchingService
    {
        private readonly IResumeSourcingService _sourcingService;
        private readonly IResumeParsingService _parsingService;

        public ResumeMatchingService(IResumeSourcingService sourcingService, IResumeParsingService parsingService)
        {
            _sourcingService = sourcingService;
            _parsingService = parsingService;
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
                    // Content = r.Resume.Content,
                    EmailSubject = r.Resume.EmailSubject,
                    EmailSender = r.Resume.EmailSender,
                    Email = r.Resume.Email,
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
            
            return new ResumeMatchingResponse
            {
                Rankings = summaryRankings,
                TotalResumesProcessed = resumes.Count,
                JobDescription = request.JobDescription,
                ExtractedKeywords = await _parsingService.ExtractKeywordsFromJobDescriptionAsync(request.JobDescription)
            };
        }

        public async Task<List<Resume>> GetAvailableResumesAsync()
        {
            return await _sourcingService.GetAllResumesAsync(new ResumeMatchingRequest());
        }

        public async Task<ResumeRanking> GetResumeRankingAsync(string resumeId, string jobDescription)
        {
            var resumes = await _sourcingService.GetAllResumesAsync(new ResumeMatchingRequest());
            var resume = resumes.FirstOrDefault(r => r.Id == resumeId);
            if (resume == null) return new ResumeRanking();
            var keywords = await _parsingService.ExtractKeywordsFromJobDescriptionAsync(jobDescription);
            return await _parsingService.RankResumeAsync(resume, jobDescription, keywords);
        }
    }
} 