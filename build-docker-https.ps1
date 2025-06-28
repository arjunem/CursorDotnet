# Build and run Resume Matcher API with HTTPS support
Write-Host "Building Docker image with HTTPS support..." -ForegroundColor Green

# Stop and remove existing container if it exists
docker stop resume-matcher-api 2>$null
docker rm resume-matcher-api 2>$null

# Build the image
docker build -t resume-matcher-api .

if ($LASTEXITCODE -eq 0) {
    Write-Host "Docker image built successfully!" -ForegroundColor Green
    
    # Run the container with both HTTP and HTTPS ports
    Write-Host "Starting Resume Matcher API container with HTTPS..." -ForegroundColor Green
    docker run -d --name resume-matcher-api -p 8080:80 -p 8443:443 resume-matcher-api
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Container started successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "API is available at:" -ForegroundColor Yellow
        Write-Host "  HTTP:  http://localhost:8080" -ForegroundColor Cyan
        Write-Host "  HTTPS: https://localhost:8443" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Endpoints:" -ForegroundColor Yellow
        Write-Host "  Health check: http://localhost:8080/api/status/health" -ForegroundColor Cyan
        Write-Host "  Swagger UI: http://localhost:8080/swagger" -ForegroundColor Cyan
        Write-Host "  Resume matching: POST http://localhost:8080/api/resumes/fetch" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "HTTPS endpoints (self-signed certificate):" -ForegroundColor Yellow
        Write-Host "  Health check: https://localhost:8443/api/status/health" -ForegroundColor Cyan
        Write-Host "  Swagger UI: https://localhost:8443/swagger" -ForegroundColor Cyan
        Write-Host "  Resume matching: POST https://localhost:8443/api/resumes/fetch" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Note: HTTPS uses a self-signed certificate. You may see a security warning in your browser." -ForegroundColor Red
    } else {
        Write-Host "Failed to start container!" -ForegroundColor Red
    }
} else {
    Write-Host "Failed to build Docker image!" -ForegroundColor Red
} 