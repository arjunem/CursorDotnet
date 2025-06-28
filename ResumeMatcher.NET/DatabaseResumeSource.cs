namespace ResumeMatcher.NET;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using ResumeMatcher.Core.Models;

public class DatabaseResumeSource
{
    private readonly string _dbPath;

    public DatabaseResumeSource(string dbPath = "resumes.db")
    {
        _dbPath = dbPath;
    }

    public async Task<List<Resume>> FetchResumesFromDatabaseAsync()
    {
        var resumes = new List<Resume>();
        if (!File.Exists(_dbPath))
            return resumes;

        using var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
        await conn.OpenAsync();
        using var cmd = new SQLiteCommand(@"SELECT id, fileName, filePath, content, emailSubject, emailSender, email, phone, emailDate, source, createdAt, processedAt, status FROM resumes ORDER BY createdAt DESC", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            resumes.Add(new Resume
            {
                Id = reader["id"].ToString(),
                FileName = reader["fileName"].ToString(),
                FilePath = reader["filePath"].ToString(),
                Content = reader["content"]?.ToString(),
                EmailSubject = reader["emailSubject"]?.ToString(),
                EmailSender = reader["emailSender"]?.ToString(),
                Email = reader["email"]?.ToString(),
                Phone = reader["phone"]?.ToString(),
                EmailDate = DateTime.TryParse(reader["emailDate"]?.ToString(), out var emailDate) ? emailDate : (DateTime?)null,
                Source = reader["source"]?.ToString() ?? "Database",
                CreatedAt = DateTime.TryParse(reader["createdAt"].ToString(), out var created) ? created : DateTime.UtcNow,
                ProcessedAt = DateTime.TryParse(reader["processedAt"]?.ToString(), out var processed) ? processed : (DateTime?)null,
                Status = Enum.TryParse(reader["status"].ToString(), out ResumeStatus status) ? status : ResumeStatus.Pending
            });
        }
        return resumes;
    }
}
