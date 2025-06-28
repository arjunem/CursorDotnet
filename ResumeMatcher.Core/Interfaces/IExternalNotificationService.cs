using ResumeMatcher.Core.Models;

namespace ResumeMatcher.Core.Interfaces
{
    public interface IExternalNotificationService
    {
        Task SendResumeNotificationAsync(List<ResumeSummary> topResumes, string jobDescription, string? jobTitle = null);
    }
} 