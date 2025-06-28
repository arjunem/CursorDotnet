using System.ComponentModel.DataAnnotations;

namespace ResumeMatcher.Core.Models
{
    public class Resume
    {
        [Required]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string FileName { get; set; } = string.Empty;
        
        [Required]
        public string FilePath { get; set; } = string.Empty;
        
        public string? Content { get; set; }
        
        public string? EmailSubject { get; set; }
        
        public string? EmailSender { get; set; }
        
        public string? Email { get; set; } // Extracted email from resume content
        
        public string? Phone { get; set; } // Extracted phone number from resume content
        
        public DateTime? EmailDate { get; set; }
        
        public string Source { get; set; } = string.Empty; // "Email" or "Database"
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ProcessedAt { get; set; }
        
        public ResumeStatus Status { get; set; } = ResumeStatus.Pending;
    }
    
    public enum ResumeStatus
    {
        Pending,
        Processing,
        Processed,
        Failed
    }
} 