# Test script for external notification functionality
Write-Host "Testing Resume Matcher API with external notification..." -ForegroundColor Green

$requestBody = @{
    jobDescription = "Senior Software Engineer with experience in C#, .NET, and web development"
    jobTitle = "Senior Software Engineer"
    excludeUnmatched = $false
    fetchUnreadEmailsOnly = $false
} | ConvertTo-Json

Write-Host "Request Body:" -ForegroundColor Yellow
Write-Host $requestBody

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/resumes/fetch" -Method POST -Body $requestBody -ContentType "application/json"
    
    Write-Host "`nAPI Response:" -ForegroundColor Green
    Write-Host "Total resumes processed: $($response.totalResumesProcessed)"
    Write-Host "Job Title: $($response.jobTitle)"
    Write-Host "Job Description: $($response.jobDescription)"
    Write-Host "Top 2 resumes that would be sent in notification:" -ForegroundColor Yellow
    
    $top2 = $response.rankings | Select-Object -First 2
    foreach ($ranking in $top2) {
        Write-Host "`nResume ID: $($ranking.resume.id)" -ForegroundColor Cyan
        Write-Host "Name: $($ranking.resume.name)" -ForegroundColor White
        Write-Host "Email: $($ranking.resume.email)" -ForegroundColor White
        Write-Host "Phone: $($ranking.resume.phone)" -ForegroundColor White
        Write-Host "Score: $($ranking.score)" -ForegroundColor Green
        Write-Host "Content preview: $($ranking.resume.content.Substring(0, [Math]::Min(100, $ranking.resume.content.Length)))..." -ForegroundColor Gray
    }
    
    Write-Host "`nCheck the console logs for notification details:" -ForegroundColor Yellow
    Write-Host "- Job Title: $($response.jobTitle)" -ForegroundColor White
    Write-Host "- Target phone numbers: +919562021296, +918086241449" -ForegroundColor White
    Write-Host "- Notification URL: https://api.example.com/notify (configurable)" -ForegroundColor White
    Write-Host "- Fire and forget: The notification is sent asynchronously" -ForegroundColor White
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Make sure the API is running on http://localhost:5000" -ForegroundColor Yellow
} 