# Test API Integration
$body = @{
    jobDescription = "Senior Software Engineer with 5+ years of experience in C#, .NET, and SQL Server"
    requiredSkills = @("C#", ".NET", "SQL Server")
    includeEmailResumes = $true
    includeDatabaseResumes = $true
} | ConvertTo-Json

Write-Host "Testing API with body: $body"

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5156/api/ResumeMatching" -Method POST -ContentType "application/json" -Body $body
    Write-Host "API Response:"
    $response | ConvertTo-Json -Depth 10
} catch {
    Write-Host "Error calling API: $($_.Exception.Message)"
    Write-Host "Response: $($_.Exception.Response)"
} 