using ResumeMatcher.Core.Interfaces;
using ResumeMatcher.Core.Models;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace ResumeMatcher.Services
{
    public class ExternalNotificationService : IExternalNotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _notificationUrl;
        private readonly string[] _hardcodedEmails = { "nayanas@triassicsolutions.com", "navas.jaseer@triassicsolutions.com" };
        private readonly string[] _hardcodedPhones = { "+918086241449", "+919562021296" };

        public ExternalNotificationService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _notificationUrl = configuration["ExternalNotification:Url"] ?? "https://api.example.com/notify";
        }

        public async Task SendResumeNotificationAsync(List<ResumeSummary> topResumes, string jobDescription, string? jobTitle = null, string? promptId = null)
        {
            try
            {
                // Take only the top 2 resumes
                var top2Resumes = topResumes.Take(2).ToList();
                
                if (!top2Resumes.Any())
                {
                    Console.WriteLine("[ExternalNotificationService] No resumes to send");
                    return;
                }

                // Create the notification payload with hardcoded email and phone
                var notificationPayload = new
                {
                    promptId = promptId,
                    jobDescription = jobDescription,
                    jobTitle = jobTitle,
                    resumes = top2Resumes.Select((r, i) => new
                    {
                        id = r.Id,
                        name = r.Name,
                        email = i < 2 ? _hardcodedEmails[i] : r.Email,
                        phone = i < 2 ? _hardcodedPhones[i] : r.Phone,
                        content = r.Content
                    }).ToList(),
                    timestamp = DateTime.UtcNow,
                    totalResumesFound = top2Resumes.Count
                };

                var json = JsonSerializer.Serialize(notificationPayload, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                // Write payload to file in root directory for verification
                var rootPath = Directory.GetCurrentDirectory();
                while (!File.Exists(Path.Combine(rootPath, "ResumeMatcher.sln")))
                {
                    var parent = Directory.GetParent(rootPath);
                    if (parent == null)
                    {
                        rootPath = Directory.GetCurrentDirectory();
                        break;
                    }
                    rootPath = parent.FullName;
                }
                
                var fileName = $"notification_payload_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var filePath = Path.Combine(rootPath, fileName);
                await File.WriteAllTextAsync(filePath, json);
                Console.WriteLine($"[ExternalNotificationService] Payload written to file: {filePath}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"[ExternalNotificationService] Sending notification for {top2Resumes.Count} resumes");
                Console.WriteLine($"[ExternalNotificationService] Payload: {json}");

                // Fire and forget - don't await the response
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var response = await _httpClient.PostAsync(_notificationUrl, content);
                        Console.WriteLine($"[ExternalNotificationService] Notification sent successfully. Status: {response.StatusCode}");
                        
                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"[ExternalNotificationService] Response: {responseContent}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ExternalNotificationService] Error sending notification: {ex.Message}");
                    }
                });

                Console.WriteLine("[ExternalNotificationService] Notification request queued (fire and forget)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExternalNotificationService] Error preparing notification: {ex.Message}");
            }
        }
    }
} 