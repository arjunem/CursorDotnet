using System.IO;

namespace ResumeMatcher.Core.Models
{
    public class ResumeRanking
    {
        public Resume Resume { get; set; } = new Resume();
        
        public double Score { get; set; }
        
        public int Rank { get; set; }
        
        public List<KeywordMatch> KeywordMatches { get; set; } = new List<KeywordMatch>();
        
        // public double SkillsMatchPercentage { get; set; }
        
        // public double ExperienceMatchPercentage { get; set; }
        
        // public double EducationMatchPercentage { get; set; }
        
        public string? Summary { get; set; }
        
        // Additional candidate information for easier access
        public string? CandidateEmail => Resume.Email;
        public string? CandidatePhone => Resume.Phone; // Phone number from resume
        public string? CandidateEmailSender => Resume.EmailSender;
        public string? CandidateEmailSubject => Resume.EmailSubject;
        public DateTime? CandidateEmailDate => Resume.EmailDate;
        public string? ResumeContent => Resume.Content;
        public string CandidateName => ExtractCandidateName();
        public string ResumeSource { get; set; } = string.Empty;
        
        private string ExtractCandidateName()
        {
            // Try to extract name from email sender first
            if (!string.IsNullOrEmpty(Resume.EmailSender))
            {
                var sender = Resume.EmailSender;
                // Remove email address part if present
                if (sender.Contains("<"))
                {
                    sender = sender.Substring(0, sender.IndexOf("<")).Trim();
                }
                // Remove quotes if present
                sender = sender.Replace("\"", "").Trim();
                if (!string.IsNullOrEmpty(sender))
                {
                    return sender;
                }
            }
            
            // Fallback to filename without extension
            if (!string.IsNullOrEmpty(Resume.FileName))
            {
                var fileName = Path.GetFileNameWithoutExtension(Resume.FileName);
                // Replace underscores and hyphens with spaces
                fileName = fileName.Replace("_", " ").Replace("-", " ");
                return fileName;
            }
            
            return "Unknown Candidate";
        }
    }
    
    public class KeywordMatch
    {
        public string Keyword { get; set; } = string.Empty;
        
        public double Weight { get; set; }
        
        public List<string> Context { get; set; } = new List<string>();
    }
} 