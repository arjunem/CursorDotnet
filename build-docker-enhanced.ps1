# Enhanced Resume Matcher API Docker Build Script
# This script provides multiple build options and better error handling

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("dev", "prod", "test")]
    [string]$Environment = "dev",
    
    [Parameter(Mandatory=$false)]
    [switch]$NoCache,
    
    [Parameter(Mandatory=$false)]
    [switch]$Push,
    
    [Parameter(Mandatory=$false)]
    [string]$Tag = "latest"
)

Write-Host "=== Resume Matcher API Docker Build Script ===" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host "No Cache: $NoCache" -ForegroundColor Yellow
Write-Host "Tag: $Tag" -ForegroundColor Yellow

# Function to check if Docker is running
function Test-DockerRunning {
    try {
        docker version | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

# Function to check if port is available
function Test-PortAvailable {
    param([int]$Port)
    try {
        $connection = New-Object System.Net.Sockets.TcpClient
        $connection.Connect("localhost", $Port)
        $connection.Close()
        return $false
    }
    catch {
        return $true
    }
}

# Check if Docker is running
if (-not (Test-DockerRunning)) {
    Write-Host "Docker is not running. Please start Docker Desktop first." -ForegroundColor Red
    exit 1
}

# Stop and remove existing container if it exists
Write-Host "Cleaning up existing containers..." -ForegroundColor Green
docker stop resume-matcher-api 2>$null
docker rm resume-matcher-api 2>$null

# Build arguments
$buildArgs = @()
if ($NoCache) {
    $buildArgs += "--no-cache"
}

# Build the Docker image
Write-Host "Building Docker image for environment: $Environment..." -ForegroundColor Green
$buildCommand = "docker build -t resume-matcher-api:$Tag"
if ($buildArgs.Count -gt 0) {
    $buildCommand += " " + ($buildArgs -join " ")
}
$buildCommand += " ."

Write-Host "Executing: $buildCommand" -ForegroundColor Gray
Invoke-Expression $buildCommand

if ($LASTEXITCODE -eq 0) {
    Write-Host "Docker image built successfully!" -ForegroundColor Green
    
    # Run the container based on environment
    switch ($Environment) {
        "dev" {
            Write-Host "Starting development container..." -ForegroundColor Yellow
            
            # Check if ports are available
            if (-not (Test-PortAvailable -Port 8080)) {
                Write-Host "Port 8080 is in use. Using port 8081 instead." -ForegroundColor Yellow
                $port = "8081:80"
            } else {
                $port = "8080:80"
            }
            
            docker run -d `
                --name resume-matcher-api `
                -p $port `
                -v "${PWD}/resumes.db:/app/resumes.db" `
                -v "${PWD}/temp_resumes:/app/temp_resumes" `
                -v "${PWD}/PythonScripts:/app/PythonScripts" `
                -v "${PWD}/logs:/app/logs" `
                --restart unless-stopped `
                resume-matcher-api:$Tag
        }
        "prod" {
            Write-Host "Starting production container..." -ForegroundColor Yellow
            
            if (-not (Test-PortAvailable -Port 80)) {
                Write-Host "Port 80 is in use. Please stop the service using port 80 first." -ForegroundColor Red
                exit 1
            }
            
            docker run -d `
                --name resume-matcher-api `
                -p "80:80" `
                -v "${PWD}/resumes.db:/app/resumes.db" `
                -v "${PWD}/temp_resumes:/app/temp_resumes" `
                -v "${PWD}/PythonScripts:/app/PythonScripts" `
                -v "${PWD}/logs:/app/logs" `
                --restart always `
                resume-matcher-api:$Tag
        }
        "test" {
            Write-Host "Starting test container..." -ForegroundColor Yellow
            
            if (-not (Test-PortAvailable -Port 8082)) {
                Write-Host "Port 8082 is in use. Using port 8083 instead." -ForegroundColor Yellow
                $port = "8083:80"
            } else {
                $port = "8082:80"
            }
            
            docker run -d `
                --name resume-matcher-api-test `
                -p $port `
                -v "${PWD}/resumes.db:/app/resumes.db" `
                -v "${PWD}/temp_resumes:/app/temp_resumes" `
                -v "${PWD}/PythonScripts:/app/PythonScripts" `
                --restart unless-stopped `
                resume-matcher-api:$Tag
        }
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Container started successfully!" -ForegroundColor Green
        
        # Determine the correct port
        $containerPort = switch ($Environment) {
            "dev" { if (Test-PortAvailable -Port 8080) { "8080" } else { "8081" } }
            "prod" { "80" }
            "test" { if (Test-PortAvailable -Port 8082) { "8082" } else { "8083" } }
        }
        
        Write-Host "API is available at: http://localhost:$containerPort" -ForegroundColor Cyan
        Write-Host "Health check: http://localhost:$containerPort/api/status/health" -ForegroundColor Cyan
        Write-Host "Swagger UI: http://localhost:$containerPort/swagger" -ForegroundColor Cyan
        
        Write-Host "`nContainer logs:" -ForegroundColor Yellow
        $containerName = if ($Environment -eq "test") { "resume-matcher-api-test" } else { "resume-matcher-api" }
        docker logs $containerName
        
        # Wait a moment and check health
        Start-Sleep -Seconds 5
        Write-Host "`nHealth check:" -ForegroundColor Yellow
        try {
            $healthResponse = Invoke-RestMethod -Uri "http://localhost:$containerPort/api/status/health" -Method Get -TimeoutSec 10
            Write-Host "Health check passed: $($healthResponse.status)" -ForegroundColor Green
        }
        catch {
            Write-Host "Health check failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    } else {
        Write-Host "Failed to start container!" -ForegroundColor Red
    }
} else {
    Write-Host "Failed to build Docker image!" -ForegroundColor Red
    exit 1
}

Write-Host "`n=== Build Complete ===" -ForegroundColor Cyan 