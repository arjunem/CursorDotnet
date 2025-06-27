using ResumeMatcher.Core.Models;

namespace ResumeMatcher.Core.Interfaces
{
    public interface IResumeSourcingService
    {
        Task<List<Resume>> GetResumesFromEmailAsync(string subjectFilter = "resume", List<string> attachmentExtensions = null);
        Task<List<Resume>> GetResumesFromDatabaseAsync();
        Task<List<Resume>> GetAllResumesAsync(ResumeMatchingRequest request);
    }
} 