using ResumeMatcher.Core.Models;

namespace ResumeMatcher.Core.Interfaces
{
    public interface IResumeParsingService
    {
        Task<string> ExtractTextFromResumeAsync(Resume resume);
        Task<List<string>> ExtractKeywordsFromJobDescriptionAsync(string jobDescription);
        Task<ResumeRanking> RankResumeAsync(Resume resume, string jobDescription, List<string> requiredSkills, List<string> preferredSkills);
        Task<List<ResumeRanking>> RankResumesAsync(List<Resume> resumes, ResumeMatchingRequest request);
    }
} 