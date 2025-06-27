using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ResumeMatcher.Core.Models
{
    public class ResumeMatchingRequest
    {
        [Required]
        public string JobDescription { get; set; } = string.Empty;
        
        public string? JobTitle { get; set; }
        
        public string? Company { get; set; }
        
        public List<string>? RequiredSkills { get; set; }
        
        public List<string>? PreferredSkills { get; set; }
        
        public int? MaxResults { get; set; } = 10;
        
        public bool IncludeEmailResumes { get; set; } = true;
        
        public bool IncludeDatabaseResumes { get; set; } = true;
    }
    
    public class ResumeSummary
    {
        public string Id { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        // public string? Content { get; set; }
        public string? EmailSubject { get; set; }
        public string? EmailSender { get; set; }
        public string? Email { get; set; }
        public DateTime? EmailDate { get; set; }
        public string Source { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public ResumeStatus Status { get; set; } = ResumeStatus.Pending;
    }
    
    public class ResumeRankingSummary
    {
        public ResumeSummary Resume { get; set; } = new ResumeSummary();
        public double Score { get; set; }
        public int Rank { get; set; }
        public List<KeywordMatch> KeywordMatches { get; set; } = new List<KeywordMatch>();
        // public double SkillsMatchPercentage { get; set; }
        // public double ExperienceMatchPercentage { get; set; }
        // public double EducationMatchPercentage { get; set; }
        public string? Summary { get; set; }
        public string ResumeSource { get; set; } = string.Empty;
    }
    
    public class ResumeMatchingResponse
    {
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
        
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        
        public int TotalResumesProcessed { get; set; }
        
        public List<ResumeRankingSummary> Rankings { get; set; } = new List<ResumeRankingSummary>();
        
        public string JobDescription { get; set; } = string.Empty;
        
        public List<string> ExtractedKeywords { get; set; } = new List<string>();
        
        // Separate counts and candidate lists by source
        public int EmailResumesCount => Rankings.Count(r => r.ResumeSource == "Email");
        public int DatabaseResumesCount => Rankings.Count(r => r.ResumeSource == "Database");
        public List<string> EmailCandidates => Rankings.Where(r => r.ResumeSource == "Email").Select(r => r.Resume.Email).Where(e => !string.IsNullOrEmpty(e)).Distinct().ToList();
        public List<string> DatabaseCandidates => Rankings.Where(r => r.ResumeSource == "Database").Select(r => r.Resume.Email).Where(e => !string.IsNullOrEmpty(e)).Distinct().ToList();
        public List<string> AllCandidates => Rankings.Select(r => r.Resume.Email).Where(e => !string.IsNullOrEmpty(e)).Distinct().ToList();
        // public double AverageScore => Rankings.Any() ? Rankings.Average(r => r.Score) : 0.0;
        // public double MaxScore => Rankings.Any() ? Rankings.Max(r => r.Score) : 0.0;
        // public double MinScore => Rankings.Any() ? Rankings.Min(r => r.Score) : 0.0;
    }
} 