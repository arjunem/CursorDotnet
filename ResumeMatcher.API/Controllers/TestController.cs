using Microsoft.AspNetCore.Mvc;
using ResumeMatcher.Core.Interfaces;
using ResumeMatcher.Core.Models;
using ResumeMatcher.NET;

namespace ResumeMatcher.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IResumeSourcingService _resumeSourcingService;
        private readonly ResumeParser _dotNetParser;

        public TestController(IResumeSourcingService resumeSourcingService)
        {
            _resumeSourcingService = resumeSourcingService;
            _dotNetParser = new ResumeParser();
        }

        [HttpGet("email-fetch")]
        public async Task<IActionResult> TestEmailFetch()
        {
            try
            {
                var resumes = await _resumeSourcingService.GetResumesFromEmailAsync();
                return Ok(new { 
                    success = true, 
                    count = resumes.Count, 
                    resumes = resumes.Select(r => new { 
                        r.Id, 
                        r.FileName, 
                        r.EmailSubject, 
                        r.EmailSender, 
                        r.Email,
                        r.EmailDate, 
                        r.Source,
                        // contentPreview = r.Content?.Substring(0, Math.Min(100, r.Content?.Length ?? 0)) + "..."
                    }).ToList() 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet("db-fetch")]
        public async Task<IActionResult> TestDatabaseFetch()
        {
            try
            {
                var resumes = await _resumeSourcingService.GetResumesFromDatabaseAsync();
                return Ok(new { 
                    success = true, 
                    count = resumes.Count, 
                    resumes = resumes.Select(r => new { 
                        r.Id, 
                        r.FileName, 
                        r.EmailSubject, 
                        r.EmailSender, 
                        r.Email,
                        r.EmailDate, 
                        r.Source,
                        // contentPreview = r.Content?.Substring(0, Math.Min(100, r.Content?.Length ?? 0)) + "..."
                    }).ToList() 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet("parser-test")]
        public IActionResult TestParserLogic()
        {
            try
            {
                // Test 1: Keyword extraction from job description
                var jobDescription = "Senior Software Engineer with 5+ years of experience in .NET, C#, and web development. Must have experience with ASP.NET Core, SQL Server, and cloud technologies.";
                var extractedKeywords = _dotNetParser.ExtractKeywordsFromJobDescription(jobDescription);
                
                // Test 2: Keyword matching with sample resume text
                var sampleResumeText = "John Doe\nSoftware Engineer\n5 years experience in C#, .NET, SQL Server\nSkills: C#, .NET, SQL Server, JavaScript, React";
                var keywords = new List<string> { ".NET", "C#", "ASP.NET Core", "SQL Server", "Azure", "JavaScript", "React", "Git" };
                var keywordMatches = _dotNetParser.CalculateKeywordMatches(sampleResumeText, keywords);
                
                // Test 3: Skills match percentage
                // var skillsMatchPercentage = _dotNetParser.CalculateSkillsMatchPercentage(sampleResumeText, keywords);
                
                // Test 4: Create a sample resume with candidate details
                var sampleResume = new Resume
                {
                    Id = "test_001",
                    FileName = "john_doe_resume.pdf",
                    FilePath = "/sample/resumes/john_doe_resume.pdf",
                    // Content = sampleResumeText,
                    EmailSubject = "Application for Senior Software Engineer Position",
                    EmailSender = "\"John Doe\" <john.doe@example.com>",
                    EmailDate = DateTime.Now.AddDays(-1),
                    Source = "Email",
                    CreatedAt = DateTime.UtcNow,
                    Status = ResumeStatus.Processed
                };
                
                // Test 5: Create a sample ranking with candidate details
                var sampleRanking = new ResumeRanking
                {
                    Resume = sampleResume,
                    Score = keywordMatches.Sum(km => km.Weight),
                    Rank = 1,
                    KeywordMatches = keywordMatches
                    // SkillsMatchPercentage = skillsMatchPercentage
                };
                
                var test2_keywordExtraction = new
                {
                    jobDescription = jobDescription,
                    keywordCount = extractedKeywords.Count,
                    keywords = extractedKeywords
                };
                
                var test3_keywordMatching = new
                {
                    resumeText = sampleResumeText,
                    keywords = keywords,
                    // keywordMatches = keywordMatches.Select(km => new { km.Keyword, km.Count, km.Weight }),
                    keywordMatches = keywordMatches.Select(km => new { km.Keyword, km.Weight }),
                    // totalMatches = keywordMatches.Count,
                    totalMatches = keywordMatches.Count,
                    totalScore = keywordMatches.Sum(km => km.Weight)
                };
                
                return Ok(new
                {
                    success = true,
                    test1_keywordExtraction = new
                    {
                        jobDescription,
                        extractedKeywords,
                        keywordCount = extractedKeywords.Count
                    },
                    test2_keywordExtraction = test2_keywordExtraction,
                    test3_skillsMatch = new
                    {
                        // skillsMatchPercentage,
                        matchedSkills = keywordMatches.Select(km => km.Keyword).ToList()
                    },
                    test4_candidateDetails = new
                    {
                        candidateName = sampleRanking.CandidateName,
                        candidateEmail = sampleRanking.CandidateEmail,
                        candidateEmailSender = sampleRanking.CandidateEmailSender,
                        candidateEmailSubject = sampleRanking.CandidateEmailSubject,
                        candidateEmailDate = sampleRanking.CandidateEmailDate,
                        // resumeContent = sampleRanking.ResumeContent,
                        // content = sampleRanking.ResumeContent,
                        resumeSource = sampleRanking.ResumeSource,
                        resumeFileName = sampleResume.FileName,
                        resumeId = sampleResume.Id
                    },
                    test5_sampleRanking = new
                    {
                        resume = sampleRanking.Resume,
                        score = sampleRanking.Score,
                        rank = sampleRanking.Rank,
                        // skillsMatchPercentage = sampleRanking.SkillsMatchPercentage,
                        // keywordMatches = sampleRanking.KeywordMatches.Select(km => new { km.Keyword, km.Count, km.Weight, km.Context }),
                        keywordMatches = sampleRanking.KeywordMatches.Select(km => new { km.Keyword, km.Weight }),
                        candidateInfo = new
                        {
                            name = sampleRanking.CandidateName,
                            email = sampleRanking.CandidateEmail,
                            emailSender = sampleRanking.CandidateEmailSender,
                            emailSubject = sampleRanking.CandidateEmailSubject,
                            emailDate = sampleRanking.CandidateEmailDate,
                            source = sampleRanking.ResumeSource
                        },
                        resumeInfo = new
                        {
                            // content = sampleRanking.ResumeContent,
                            fileName = sampleResume.FileName,
                            id = sampleResume.Id
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }
} 