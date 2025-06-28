using ResumeMatcher.Core.Models;

namespace ResumeMatcher.Core.Interfaces
{
    public interface IResumeMatchingService
    {
        Task<ResumeMatchingResponse> MatchResumesAsync(ResumeMatchingRequest request);
        Task<List<Resume>> GetAvailableResumesAsync();
        Task<ResumeRanking> GetResumeRankingAsync(string resumeId, string jobDescription, bool useOllama = false);
    }
} 