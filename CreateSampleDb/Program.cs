using System;
using System.Data.SQLite;
using System.IO;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string dbPath = "resumes.db";
        
        // Delete existing database if it exists
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
            Console.WriteLine("Deleted existing database.");
        }

        // Create new database
        SQLiteConnection.CreateFile(dbPath);
        Console.WriteLine($"Created new database: {dbPath}");

        using var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
        connection.Open();

        // Create table with email field
        string createTableSql = @"
            CREATE TABLE resumes (
                id TEXT PRIMARY KEY,
                fileName TEXT NOT NULL,
                filePath TEXT NOT NULL,
                content TEXT,
                emailSubject TEXT,
                emailSender TEXT,
                email TEXT,
                emailDate TEXT,
                source TEXT NOT NULL,
                createdAt TEXT NOT NULL,
                processedAt TEXT,
                status INTEGER DEFAULT 0
            );";

        using var createCommand = new SQLiteCommand(createTableSql, connection);
        createCommand.ExecuteNonQuery();
        Console.WriteLine("Created resumes table.");

        // Helper function to extract email from content
        string ExtractEmailFromContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return null;
            
            // Regex pattern to find email addresses
            var emailPattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b";
            var match = Regex.Match(content, emailPattern);
            return match.Success ? match.Value : null;
        }

        // Sample resumes with email information
        var sampleResumes = new[]
        {
            new {
                Id = Guid.NewGuid().ToString(),
                FileName = "john_doe_resume.pdf",
                FilePath = "/sample/resumes/john_doe_resume.pdf",
                Content = @"John Doe
Senior Software Engineer
Email: john.doe@example.com
Phone: (555) 123-4567
5 years experience in C#, .NET, SQL Server
Skills: C#, .NET, SQL Server, JavaScript, React, Azure
Experience: Full-stack development, API design, database optimization",
                EmailSubject = "Application for Senior Software Engineer Position",
                EmailSender = "\"John Doe\" <john.doe@example.com>",
                EmailDate = DateTime.Now.AddDays(-5),
                Source = "Database"
            },
            new {
                Id = Guid.NewGuid().ToString(),
                FileName = "jane_smith_resume.docx",
                FilePath = "/sample/resumes/jane_smith_resume.docx",
                Content = @"Jane Smith
Senior Developer
Contact: jane.smith@techcorp.com
LinkedIn: linkedin.com/in/janesmith
8 years experience in Python, Django, PostgreSQL
Skills: Python, Django, PostgreSQL, Docker, AWS, Kubernetes
Experience: Backend development, DevOps, cloud architecture",
                EmailSubject = "Resume - Senior Developer Role",
                EmailSender = "\"Jane Smith\" <jane.smith@techcorp.com>",
                EmailDate = DateTime.Now.AddDays(-3),
                Source = "Database"
            },
            new {
                Id = Guid.NewGuid().ToString(),
                FileName = "mike_johnson_resume.pdf",
                FilePath = "/sample/resumes/mike_johnson_resume.pdf",
                Content = @"Mike Johnson
Full Stack Developer
Email: mike.johnson@startup.io
Website: mikejohnson.dev
6 years experience in Java, Spring, MySQL
Skills: Java, Spring, MySQL, Angular, Git, Jenkins
Experience: Enterprise applications, CI/CD pipelines",
                EmailSubject = "Fwd: Resume - Full Stack Developer",
                EmailSender = "\"Mike Johnson\" <mike.johnson@startup.io>",
                EmailDate = DateTime.Now.AddDays(-2),
                Source = "Database"
            },
            new {
                Id = Guid.NewGuid().ToString(),
                FileName = "sarah_wilson_resume.pdf",
                FilePath = "/sample/resumes/sarah_wilson_resume.pdf",
                Content = @"Sarah Wilson
Software Engineer
Contact Information:
Email: sarah.wilson@innovate.com
Phone: (555) 987-6543
4 years experience in .NET, C#, Azure
Skills: C#, ASP.NET Core, Azure, SQL Server, TypeScript, React
Experience: Web development, cloud services, agile methodologies",
                EmailSubject = "Application - Software Engineer Position",
                EmailSender = "\"Sarah Wilson\" <sarah.wilson@innovate.com>",
                EmailDate = DateTime.Now.AddDays(-1),
                Source = "Database"
            },
            new {
                Id = Guid.NewGuid().ToString(),
                FileName = "david_brown_resume.docx",
                FilePath = "/sample/resumes/david_brown_resume.docx",
                Content = @"David Brown
Senior .NET Developer
Email: david.brown@enterprise.com
GitHub: github.com/davidbrown
7 years experience in .NET, C#, SQL Server
Skills: .NET, C#, SQL Server, Entity Framework, WPF, Xamarin
Experience: Desktop applications, mobile development, database design",
                EmailSubject = "Resume - Senior .NET Developer",
                EmailSender = "\"David Brown\" <david.brown@enterprise.com>",
                EmailDate = DateTime.Now.AddDays(-4),
                Source = "Database"
            },
            new {
                Id = Guid.NewGuid().ToString(),
                FileName = "emma_davis_resume.pdf",
                FilePath = "/sample/resumes/emma_davis_resume.pdf",
                Content = @"Emma Davis
Full Stack .NET Developer
Contact: emma.davis@webtech.com
Portfolio: emmadavis.dev
5 years experience in .NET, C#, JavaScript
Skills: .NET, C#, JavaScript, React, SQL Server, Azure DevOps
Experience: Web applications, API development, team leadership",
                EmailSubject = "Application for Full Stack Developer Role",
                EmailSender = "\"Emma Davis\" <emma.davis@webtech.com>",
                EmailDate = DateTime.Now.AddDays(-6),
                Source = "Database"
            },
            new {
                Id = Guid.NewGuid().ToString(),
                FileName = "alex_chen_resume.pdf",
                FilePath = "/sample/resumes/alex_chen_resume.pdf",
                Content = @"Alex Chen
Software Engineer
Email: alex.chen@modernapps.com
LinkedIn: linkedin.com/in/alexchen
3 years experience in C#, .NET, Azure
Skills: C#, .NET, Azure, SQL Server, JavaScript, Vue.js
Experience: Modern web development, cloud integration, testing",
                EmailSubject = "Resume - Software Engineer Application",
                EmailSender = "\"Alex Chen\" <alex.chen@modernapps.com>",
                EmailDate = DateTime.Now.AddDays(-7),
                Source = "Database"
            },
            new {
                Id = Guid.NewGuid().ToString(),
                FileName = "lisa_garcia_resume.docx",
                FilePath = "/sample/resumes/lisa_garcia_resume.docx",
                Content = @"Lisa Garcia
Senior Backend Developer
Contact Information:
Email: lisa.garcia@backend.com
Phone: (555) 456-7890
6 years experience in .NET, C#, SQL Server
Skills: .NET, C#, SQL Server, Entity Framework, Azure, Docker
Experience: Microservices, database optimization, performance tuning",
                EmailSubject = "Application - Senior Backend Developer",
                EmailSender = "\"Lisa Garcia\" <lisa.garcia@backend.com>",
                EmailDate = DateTime.Now.AddDays(-8),
                Source = "Database"
            },
            new {
                Id = Guid.NewGuid().ToString(),
                FileName = "robert_taylor_resume.pdf",
                FilePath = "/sample/resumes/robert_taylor_resume.pdf",
                Content = @"Robert Taylor
.NET Developer
Email: robert.taylor@legacy.com
Website: roberttaylor.net
4 years experience in C#, ASP.NET, SQL Server
Skills: C#, ASP.NET, SQL Server, JavaScript, jQuery, Bootstrap
Experience: Web development, legacy system maintenance, user support",
                EmailSubject = "Resume - .NET Developer Position",
                EmailSender = "\"Robert Taylor\" <robert.taylor@legacy.com>",
                EmailDate = DateTime.Now.AddDays(-9),
                Source = "Database"
            },
            new {
                Id = Guid.NewGuid().ToString(),
                FileName = "maria_rodriguez_resume.pdf",
                FilePath = "/sample/resumes/maria_rodriguez_resume.pdf",
                Content = @"Maria Rodriguez
Full Stack .NET Developer
Contact: maria.rodriguez@fullstack.com
GitHub: github.com/mariarodriguez
5 years experience in .NET, C#, React
Skills: .NET, C#, React, TypeScript, SQL Server, Azure
Experience: Modern web applications, responsive design, API development",
                EmailSubject = "Application for Full Stack .NET Developer",
                EmailSender = "\"Maria Rodriguez\" <maria.rodriguez@fullstack.com>",
                EmailDate = DateTime.Now.AddDays(-10),
                Source = "Database"
            },
            new {
                Id = Guid.NewGuid().ToString(),
                FileName = "james_anderson_resume.docx",
                FilePath = "/sample/resumes/james_anderson_resume.docx",
                Content = @"James Anderson
Senior Software Engineer
Email: james.anderson@senior.com
LinkedIn: linkedin.com/in/jamesanderson
8 years experience in .NET, C#, Azure
Skills: .NET, C#, Azure, SQL Server, Docker, Kubernetes, CI/CD
Experience: Cloud architecture, DevOps, team mentoring, project management",
                EmailSubject = "Resume - Senior Software Engineer Role",
                EmailSender = "\"James Anderson\" <james.anderson@senior.com>",
                EmailDate = DateTime.Now.AddDays(-11),
                Source = "Database"
            },
            new {
                Id = Guid.NewGuid().ToString(),
                FileName = "sophia_lee_resume.pdf",
                FilePath = "/sample/resumes/sophia_lee_resume.pdf",
                Content = @"Sophia Lee
.NET Developer
Contact Information:
Email: sophia.lee@junior.com
Portfolio: sophialee.dev
3 years experience in C#, ASP.NET Core, SQL Server
Skills: C#, ASP.NET Core, SQL Server, Entity Framework, JavaScript, Angular
Experience: Web development, database design, frontend integration",
                EmailSubject = "Application - .NET Developer Position",
                EmailSender = "\"Sophia Lee\" <sophia.lee@junior.com>",
                EmailDate = DateTime.Now.AddDays(-12),
                Source = "Database"
            }
        };

        // Insert sample resumes
        string insertSql = @"
            INSERT INTO resumes (id, fileName, filePath, content, emailSubject, emailSender, email, emailDate, source, createdAt, processedAt, status)
            VALUES (@id, @fileName, @filePath, @content, @emailSubject, @emailSender, @email, @emailDate, @source, @createdAt, @processedAt, @status)";

        int count = 0;
        foreach (var resume in sampleResumes)
        {
            // Extract email from content
            var extractedEmail = ExtractEmailFromContent(resume.Content);
            
            using var insertCommand = new SQLiteCommand(insertSql, connection);
            insertCommand.Parameters.AddWithValue("@id", resume.Id);
            insertCommand.Parameters.AddWithValue("@fileName", resume.FileName);
            insertCommand.Parameters.AddWithValue("@filePath", resume.FilePath);
            insertCommand.Parameters.AddWithValue("@content", resume.Content);
            insertCommand.Parameters.AddWithValue("@emailSubject", resume.EmailSubject);
            insertCommand.Parameters.AddWithValue("@emailSender", resume.EmailSender);
            insertCommand.Parameters.AddWithValue("@email", extractedEmail ?? (object)DBNull.Value);
            insertCommand.Parameters.AddWithValue("@emailDate", resume.EmailDate.ToString("yyyy-MM-dd HH:mm:ss"));
            insertCommand.Parameters.AddWithValue("@source", resume.Source);
            insertCommand.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            insertCommand.Parameters.AddWithValue("@processedAt", (object)DBNull.Value);
            insertCommand.Parameters.AddWithValue("@status", 0);

            insertCommand.ExecuteNonQuery();
            count++;
            Console.WriteLine($"Inserted resume {count}: {resume.FileName} (Email: {extractedEmail ?? "Not found"})");
        }

        // Verify the data
        using var countCommand = new SQLiteCommand("SELECT COUNT(*) FROM resumes", connection);
        var totalCount = countCommand.ExecuteScalar();
        Console.WriteLine($"\nDatabase created successfully with {totalCount} resumes.");

        // Show sample data with extracted emails
        Console.WriteLine("\nSample data with extracted emails:");
        using var selectCommand = new SQLiteCommand("SELECT id, fileName, emailSender, email, source FROM resumes LIMIT 5", connection);
        using var reader = selectCommand.ExecuteReader();
        while (reader.Read())
        {
            var extractedEmail = reader["email"]?.ToString() ?? "Not found";
            Console.WriteLine($"ID: {reader["id"]}, File: {reader["fileName"]}, Sender: {reader["emailSender"]}, Extracted Email: {extractedEmail}, Source: {reader["source"]}");
        }

        connection.Close();
        Console.WriteLine("\nDatabase creation completed!");
    }
} 