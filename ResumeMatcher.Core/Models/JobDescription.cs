using System.ComponentModel.DataAnnotations;

namespace ResumeMatcher.Core.Models
{
    public class JobDescription
    {
        [Required]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        public string? Company { get; set; }
        
        public string? Location { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public List<string> Keywords { get; set; } = new List<string>();
    }
} 