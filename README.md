# Resume Matcher API

A .NET 8.0 API for matching resumes against job descriptions using both .NET and Python-based resume parsing.

## Features

- **Resume Matching**: Match resumes against job descriptions using keyword analysis
- **Multiple Sources**: Fetch resumes from both email (IMAP) and database sources
- **Flexible Parsing**: Support for both .NET and Python-based resume text extraction
- **RESTful API**: Clean REST endpoints with Swagger documentation
- **Docker Support**: Containerized deployment for easy scaling and deployment

## Quick Start

### Option 1: Docker Deployment (Recommended)

1. **Check Docker installation:**
   ```powershell
   .\check-docker.ps1
   ```

2. **Build and run with Docker Compose:**
   ```bash
   docker-compose up --build
   ```

3. **Or use the PowerShell script:**
   ```powershell
   .\build-docker.ps1
   ```

4. **Access the API:**
   - API: http://localhost:8080
   - Swagger UI: http://localhost:8080/swagger
   - Health Check: http://localhost:8080/api/status/health

### Option 2: Local Development

1. **Prerequisites:**
   - .NET 8.0 SDK
   - Python 3.8+ (for Python-based parsing)
   - SQLite (included with .NET)

2. **Build and run:**
   ```bash
   dotnet build
   cd ResumeMatcher.API
   dotnet run
   ```

3. **Access the API:**
   - API: http://localhost:5000
   - Swagger UI: http://localhost:5000/swagger

## API Endpoints

### POST /api/resumematching/match
Match resumes against a job description.

**Request Body:**
```json
{
  "jobDescription": "Senior Software Engineer with 5+ years of experience in .NET, C#, and web development. Must have experience with ASP.NET Core, SQL Server, and cloud technologies.",
  "jobTitle": "Senior Software Engineer",
  "company": "Tech Corp",
  "requiredSkills": [".NET", "C#", "ASP.NET Core"],
  "preferredSkills": ["Azure", "React", "TypeScript"],
  "maxResults": 10,
  "includeEmailResumes": true,
  "includeDatabaseResumes": true
}
```

**Response:**
```json
{
  "requestId": "guid",
  "processedAt": "2025-01-27T18:30:00.000Z",
  "totalResumesProcessed": 23,
  "rankings": [
    {
      "resume": {
        "id": "guid",
        "fileName": "resume.pdf",
        "filePath": "/path/to/resume.pdf",
        "emailSubject": "Job Application",
        "emailSender": "candidate@email.com",
        "email": "candidate@email.com",
        "source": "Email",
        "createdAt": "2025-01-27T13:23:26",
        "status": 0
      },
      "score": 0.85,
      "rank": 1,
      "keywordMatches": [
        {
          "keyword": ".NET",
          "count": 3,
          "weight": 0.1,
          "context": ["experienced with .NET", "developed .NET applications"]
        }
      ],
      "summary": "Candidate shows proficiency in: .NET, C#, ASP.NET Core. Overall match score: 0.85.",
      "resumeSource": "Email"
    }
  ],
  "jobDescription": "Senior Software Engineer...",
  "extractedKeywords": [".NET", "C#", "ASP.NET Core", "SQL Server"],
  "emailResumesCount": 10,
  "databaseResumesCount": 13,
  "emailCandidates": ["candidate1@email.com", "candidate2@email.com"],
  "databaseCandidates": ["candidate3@email.com", "candidate4@email.com"],
  "allCandidates": ["candidate1@email.com", "candidate2@email.com", "candidate3@email.com"]
}
```

### GET /api/status
Get API status and configuration.

### GET /api/status/health
Health check endpoint for Docker containers.

## Configuration

### Email Settings (appsettings.json)
```json
{
  "EmailSettings": {
    "ImapServer": "imap.gmail.com",
    "ImapPort": 993,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "MaxEmails": 50,
    "TempDirectory": "temp_resumes"
  }
}
```

### Resume Matcher Settings
```json
{
  "ResumeMatcher": {
    "UseDotNetLogic": true,
    "PythonScriptPath": "PythonScripts/resume_parser.py"
  }
}
```

## Project Structure

```
ResumeMatcher/
├── ResumeMatcher.API/          # Web API project
├── ResumeMatcher.Core/         # Core models and interfaces
├── ResumeMatcher.Services/     # Business logic services
├── ResumeMatcher.NET/          # .NET-based resume parsing
├── PythonScripts/              # Python resume parsing scripts
├── Dockerfile                  # Docker configuration
├── docker-compose.yml          # Docker Compose configuration
├── build-docker.ps1           # Docker build script
└── README-Docker.md           # Docker documentation
```

## Docker Deployment

For detailed Docker deployment instructions, see [README-Docker.md](README-Docker.md).

### Quick Docker Commands

```bash
# Build and run
docker-compose up --build

# Run in background
docker-compose up -d --build

# View logs
docker logs resume-matcher-api

# Stop services
docker-compose down
```

## Development

### Adding New Resume Sources

1. Implement `IResumeSourcingService` interface
2. Register the service in `Program.cs`
3. Update the `ResumeSourcingService` to use the new source

### Customizing Resume Parsing

1. For .NET parsing: Modify `ResumeParser.cs`
2. For Python parsing: Update `PythonScripts/resume_parser.py`
3. Configure which parser to use in `appsettings.json`

## Troubleshooting

### Common Issues

1. **Email connection fails:**
   - Check IMAP settings in `appsettings.json`
   - Ensure app password is used for Gmail
   - Verify firewall/network settings

2. **Python parsing not working:**
   - Ensure Python 3.8+ is installed
   - Check Python script path in configuration
   - Verify required Python packages are installed

3. **Docker container won't start:**
   - Check if port 8080 is available
   - Ensure Docker Desktop is running
   - Verify volume mounts are correct

### Logs

- **Local development:** Check console output
- **Docker:** `docker logs resume-matcher-api`
- **Docker Compose:** `docker-compose logs`

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License. 