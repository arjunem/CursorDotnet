# Test script for job title functionality
Write-Host "Testing Resume Matcher API with job title..." -ForegroundColor Green

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
    Write-Host "Job Title: $($response.jobTitle)" -ForegroundColor Cyan
    Write-Host "Job Description: $($response.jobDescription)" -ForegroundColor White
    
    Write-Host "`nExternal Notification Payload will include:" -ForegroundColor Yellow
    Write-Host "- jobTitle: $($response.jobTitle)" -ForegroundColor White
    Write-Host "- jobDescription: $($response.jobDescription)" -ForegroundColor White
    Write-Host "- Top 2 resumes with hardcoded emails and phones" -ForegroundColor White
    Write-Host "- Extracted names from resume content" -ForegroundColor White
    
    Write-Host "`nCheck the notification_payload_*.txt file in root directory for the actual payload" -ForegroundColor Yellow
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Make sure the API is running on http://localhost:5000" -ForegroundColor Yellow
} 