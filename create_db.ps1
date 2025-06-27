# Create Sample Database Script
# This script runs the DBCreator and moves the database file to the root directory

Write-Host "=== Resume Database Creator ===" -ForegroundColor Green
Write-Host ""

# Check if CreateSampleDb directory exists
if (-not (Test-Path "CreateSampleDb")) {
    Write-Host "Error: CreateSampleDb directory not found!" -ForegroundColor Red
    Write-Host "Please ensure the CreateSampleDb directory exists with Program.cs and CreateSampleDb.csproj" -ForegroundColor Yellow
    exit 1
}

# Check if existing database exists in root and delete it
if (Test-Path "resumes.db") {
    Write-Host "Deleting existing database: resumes.db" -ForegroundColor Yellow
    Remove-Item "resumes.db" -Force
    Write-Host "Existing database deleted." -ForegroundColor Green
}

Write-Host "Running database creator..." -ForegroundColor Cyan

# Change to CreateSampleDb directory and run the database creator
Push-Location "CreateSampleDb"
try {
    # Build and run the database creator
    dotnet build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: Failed to build the database creator!" -ForegroundColor Red
        exit 1
    }
    
    dotnet run
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: Failed to run the database creator!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Database created successfully in CreateSampleDb directory." -ForegroundColor Green
}
finally {
    Pop-Location
}

# Check if database was created in CreateSampleDb directory
if (Test-Path "CreateSampleDb\resumes.db") {
    Write-Host "Moving database to root directory..." -ForegroundColor Cyan
    
    # Move the database file to root
    Move-Item "CreateSampleDb\resumes.db" "resumes.db" -Force
    
    # Verify the move was successful
    if (Test-Path "resumes.db") {
        $fileSize = (Get-Item "resumes.db").Length
        Write-Host "Database successfully moved to root directory!" -ForegroundColor Green
        Write-Host "File size: $($fileSize) bytes" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Database is ready for use by the API." -ForegroundColor Green
    } else {
        Write-Host "Error: Failed to move database to root directory!" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Error: Database was not created in CreateSampleDb directory!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== Database Creation Complete ===" -ForegroundColor Green
Write-Host "You can now start the API with: dotnet run --project ResumeMatcher.API" -ForegroundColor Cyan 