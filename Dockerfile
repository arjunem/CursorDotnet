# Use the official .NET 8.0 runtime as the base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the official .NET 8.0 SDK as the build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution file and restore dependencies
COPY ["ResumeMatcher.sln", "./"]
COPY ["ResumeMatcher.API/ResumeMatcher.API.csproj", "ResumeMatcher.API/"]
COPY ["ResumeMatcher.Core/ResumeMatcher.Core.csproj", "ResumeMatcher.Core/"]
COPY ["ResumeMatcher.Services/ResumeMatcher.Services.csproj", "ResumeMatcher.Services/"]
COPY ["ResumeMatcher.NET/ResumeMatcher.NET.csproj", "ResumeMatcher.NET/"]

# Copy the database file explicitly
COPY ["resumes.db", "./"]

# Restore dependencies
RUN dotnet restore "ResumeMatcher.API/ResumeMatcher.API.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/ResumeMatcher.API"
RUN dotnet build "ResumeMatcher.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "ResumeMatcher.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app

# Copy the published application
COPY --from=publish /app/publish .

# Copy the database file to the final stage
COPY --from=build /src/resumes.db /app/resumes.db

# Create directories for temp files and ensure they exist
RUN mkdir -p /app/temp_resumes
RUN mkdir -p /app/PythonScripts

# Copy Python scripts if they exist
COPY --from=build /src/PythonScripts/ /app/PythonScripts/

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port 80
EXPOSE 80

# Set the entry point
ENTRYPOINT ["dotnet", "ResumeMatcher.API.dll"] 