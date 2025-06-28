# Resume Matcher API - Docker Deployment

This document provides instructions for deploying the Resume Matcher API using Docker.

## Prerequisites

- Docker Desktop installed and running
- PowerShell (for Windows users)
- Git (to clone the repository)

## Quick Start

### Option 1: Using Docker Compose (Recommended)

1. **Build and run using docker-compose:**
   ```bash
   docker-compose up --build
   ```

2. **Run in background:**
   ```bash
   docker-compose up -d --build
   ```

3. **Stop the services:**
   ```bash
   docker-compose down
   ```

### Option 2: Using PowerShell Script

1. **Run the build script:**
   ```powershell
   .\build-docker.ps1
   ```

### Option 3: Manual Docker Commands

1. **Build the Docker image:**
   ```bash
   docker build -t resume-matcher-api .
   ```

2. **Run the container:**
   ```bash
   docker run -d \
     --name resume-matcher-api \
     -p 8080:80 \
     -v "$(pwd)/resumes.db:/app/resumes.db" \
     -v "$(pwd)/temp_resumes:/app/temp_resumes" \
     -v "$(pwd)/PythonScripts:/app/PythonScripts" \
     --restart unless-stopped \
     resume-matcher-api
   ```

## Accessing the API

Once the container is running, you can access:

- **API Base URL:** http://localhost:8080
- **Health Check:** http://localhost:8080/api/status/health
- **Swagger UI:** http://localhost:8080/swagger
- **Status Endpoint:** http://localhost:8080/api/status

## Container Management

### View logs
```bash
docker logs resume-matcher-api
```

### Follow logs in real-time
```bash
docker logs -f resume-matcher-api
```

### Stop the container
```bash
docker stop resume-matcher-api
```

### Remove the container
```bash
docker rm resume-matcher-api
```

### Remove the image
```bash
docker rmi resume-matcher-api
```

## Volume Mounts

The container uses the following volume mounts for data persistence:

- `./resumes.db` → `/app/resumes.db` (SQLite database)
- `./temp_resumes` → `/app/temp_resumes` (Temporary email attachments)
- `./PythonScripts` → `/app/PythonScripts` (Python scripts for resume parsing)

## Environment Variables

The following environment variables are set in the container:

- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://+:80`

## Health Check

The container includes a health check that verifies the API is responding:

```bash
curl http://localhost:8080/api/status/health
```

Expected response:
```json
{
  "status": "healthy",
  "timestamp": "2025-01-27T18:30:00.000Z"
}
```

## Troubleshooting

### Container won't start
1. Check if port 8080 is already in use:
   ```bash
   netstat -an | findstr :8080
   ```

2. Use a different port:
   ```bash
   docker run -d --name resume-matcher-api -p 8081:80 resume-matcher-api
   ```

### Database issues
1. Ensure the `resumes.db` file exists in the project root
2. Check file permissions on the database file
3. Verify the volume mount is working correctly

### Email functionality issues
1. Ensure the `temp_resumes` directory exists
2. Check that the Python scripts are properly mounted
3. Verify email configuration in `appsettings.json`

## Production Deployment

For production deployment, consider:

1. **Using a reverse proxy** (nginx, Apache) in front of the API
2. **Setting up SSL/TLS** certificates
3. **Using environment-specific configuration** files
4. **Implementing proper logging** and monitoring
5. **Setting up database backups** for the SQLite file

## Docker Compose Override

You can create a `docker-compose.override.yml` file for development-specific settings:

```yaml
version: '3.8'
services:
  resume-matcher-api:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    ports:
      - "8081:80"  # Use different port for development
``` 