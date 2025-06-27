@echo off
echo === Resume Database Creator ===
echo.

REM Check if CreateSampleDb directory exists
if not exist "CreateSampleDb" (
    echo Error: CreateSampleDb directory not found!
    echo Please ensure the CreateSampleDb directory exists with Program.cs and CreateSampleDb.csproj
    pause
    exit /b 1
)

REM Check if existing database exists in root and delete it
if exist "resumes.db" (
    echo Deleting existing database: resumes.db
    del "resumes.db"
    echo Existing database deleted.
)

echo Running database creator...

REM Change to CreateSampleDb directory and run the database creator
cd CreateSampleDb

REM Build the database creator
dotnet build
if errorlevel 1 (
    echo Error: Failed to build the database creator!
    cd ..
    pause
    exit /b 1
)

REM Run the database creator
dotnet run
if errorlevel 1 (
    echo Error: Failed to run the database creator!
    cd ..
    pause
    exit /b 1
)

echo Database created successfully in CreateSampleDb directory.

REM Go back to root directory
cd ..

REM Check if database was created in CreateSampleDb directory
if exist "CreateSampleDb\resumes.db" (
    echo Moving database to root directory...
    
    REM Move the database file to root
    move "CreateSampleDb\resumes.db" "resumes.db"
    
    REM Verify the move was successful
    if exist "resumes.db" (
        echo Database successfully moved to root directory!
        echo.
        echo Database is ready for use by the API.
    ) else (
        echo Error: Failed to move database to root directory!
        pause
        exit /b 1
    )
) else (
    echo Error: Database was not created in CreateSampleDb directory!
    pause
    exit /b 1
)

echo.
echo === Database Creation Complete ===
echo You can now start the API with: dotnet run --project ResumeMatcher.API
pause 