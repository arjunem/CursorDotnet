# Resume Matcher API Docker Build Script
# This script builds and runs the Resume Matcher API in a Docker container

Write-Host "Building Resume Matcher API Docker image..." -ForegroundColor Green

# Build the Docker image
docker build -t resume-matcher-api .

if ($LASTEXITCODE -eq 0) {
    Write-Host "Docker image built successfully!" -ForegroundColor Green
    
    Write-Host "Starting Resume Matcher API container..." -ForegroundColor Yellow
    
    # Run the container
    docker run -d `
        --name resume-matcher-api `
        -p 8080:80 `
        -v "${PWD}/resumes.db:/app/resumes.db" `
        -v "${PWD}/temp_resumes:/app/temp_resumes" `
        -v "${PWD}/PythonScripts:/app/PythonScripts" `
        --restart unless-stopped `
        resume-matcher-api
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Container started successfully!" -ForegroundColor Green
        Write-Host "API is available at: http://localhost:8080" -ForegroundColor Cyan
        Write-Host "Health check: http://localhost:8080/api/status/health" -ForegroundColor Cyan
        Write-Host "Swagger UI: http://localhost:8080/swagger" -ForegroundColor Cyan
        
        Write-Host "`nContainer logs:" -ForegroundColor Yellow
        docker logs resume-matcher-api
    } else {
        Write-Host "Failed to start container!" -ForegroundColor Red
    }
} else {
    Write-Host "Failed to build Docker image!" -ForegroundColor Red
} 