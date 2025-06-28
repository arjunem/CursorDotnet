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
                phone TEXT,
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

        // Helper function to extract phone number from content
        string ExtractPhoneFromContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return null;
            
            // Common phone number patterns including Indian numbers
            var patterns = new[]
            {
                // Indian mobile numbers: +91 98765 43210, +91-98765-43210, 98765 43210, 9876543210
                @"(?:\+91[\s-]?)?[6-9]\d{9}",
                // Indian landline: +91 11 2345 6789, 011-2345-6789, 011 2345 6789
                @"(?:\+91[\s-]?)?(?:0)?[1-9]\d{1,4}[\s-]?\d{3,4}[\s-]?\d{4}",
                // US patterns: (555) 123-4567, 555-123-4567, 555.123.4567
                @"\(\d{3}\)\s*\d{3}-\d{4}",
                @"\d{3}-\d{3}-\d{4}",
                @"\d{3}\.\d{3}\.\d{4}",
                @"\d{3}\s\d{3}\s\d{4}",
                @"\b\d{10}\b",
                // International: +1 555 123 4567
                @"\+\d{1,3}\s*\d{3}\s*\d{3}\s*\d{4}",
                @"1-\d{3}-\d{3}-\d{4}"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(content, pattern);
                if (match.Success)
                {
                    return match.Value.Trim();
                }
            }

            return null;
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
                FileName = "priya_sharma_resume.docx",
                FilePath = "/sample/resumes/priya_sharma_resume.docx",
                Content = @"Priya Sharma
Senior Developer
Contact: priya.sharma@techcorp.in
Phone: +91 98765 43210
LinkedIn: linkedin.com/in/priyasharma
8 years experience in Python, Django, PostgreSQL
Skills: Python, Django, PostgreSQL, Docker, AWS, Kubernetes
Experience: Backend development, DevOps, cloud architecture",
                EmailSubject = "Resume - Senior Developer Role",
                EmailSender = "\"Priya Sharma\" <priya.sharma@techcorp.in>",
                EmailDate = DateTime.Now.AddDays(-3),
                Source = "Database"
            },
            new {
                Id = Guid.NewGuid().ToString(),
                FileName = "raj_patel_resume.pdf",
                FilePath = "/sample/resumes/raj_patel_resume.pdf",
                Content = @"Raj Patel
Full Stack Developer
Email: raj.patel@startup.in
Phone: 9876543210
Website: rajpatel.dev
6 years experience in Java, Spring, MySQL
Skills: Java, Spring, MySQL, Angular, Git, Jenkins
Experience: Enterprise applications, CI/CD pipelines",
                EmailSubject = "Fwd: Resume - Full Stack Developer",
                EmailSender = "\"Raj Patel\" <raj.patel@startup.in>",
                EmailDate = DateTime.Now.AddDays(-2),
                Source = "Database"
            },
            new {
                Id = Guid.NewGuid().ToString(),
                FileName = "anita_verma_resume.pdf",
                FilePath = "/sample/resumes/anita_verma_resume.pdf",
                Content = @"Anita Verma
Software Engineer
Contact Information:
Email: anita.verma@innovate.in
Phone: +91-98765-43210
4 years experience in .NET, C#, Azure
Skills: C#, ASP.NET Core, Azure, SQL Server, TypeScript, React
Experience: Web development, cloud services, agile methodologies",
                EmailSubject = "Application - Software Engineer Position",
                EmailSender = "\"Anita Verma\" <anita.verma@innovate.in>",
                EmailDate = DateTime.Now.AddDays(-1),
                Source = "Database"
            },
            new {
                Id = Guid.NewGuid().ToString(),
                FileName = "vikram_singh_resume.docx",
                FilePath = "/sample/resumes/vikram_singh_resume.docx",
                Content = @"Vikram Singh
Senior .NET Developer
Email: vikram.singh@enterprise.in
Phone: 011-2345-6789
GitHub: github.com/vikramsingh
7 years experience in .NET, C#, SQL Server
Skills: .NET, C#, SQL Server, Entity Framework, WPF, Xamarin
Experience: Desktop applications, mobile development, database design",
                EmailSubject = "Resume - Senior .NET Developer",
                EmailSender = "\"Vikram Singh\" <vikram.singh@enterprise.in>",
                EmailDate = DateTime.Now.AddDays(-4),
                Source = "Database"
            },
            new {
                Id = Guid.NewGuid().ToString(),
                FileName = "meera_kumar_resume.pdf",
                FilePath = "/sample/resumes/meera_kumar_resume.pdf",
                Content = @"Meera Kumar
Full Stack .NET Developer
Contact: meera.kumar@webtech.in
Phone: +91 98765 43210
Portfolio: meerakumar.dev
5 years experience in .NET, C#, JavaScript
Skills: .NET, C#, JavaScript, React, SQL Server, Azure DevOps
Experience: Web applications, API development, team leadership",
                EmailSubject = "Application for Full Stack Developer Role",
                EmailSender = "\"Meera Kumar\" <meera.kumar@webtech.in>",
                EmailDate = DateTime.Now.AddDays(-6),
                Source = "Database"
            },
            new {
                Id = Guid.NewGuid().ToString(),
                FileName = "arjun_reddy_resume.pdf",
                FilePath = "/sample/resumes/arjun_reddy_resume.pdf",
                Content = @"Arjun Reddy
Software Engineer
Email: arjun.reddy@modernapps.in
Phone: 9876543210
LinkedIn: linkedin.com/in/arjunreddy
3 years experience in C#, .NET, Azure
Skills: C#, .NET, Azure, SQL Server, JavaScript, Vue.js
Experience: Modern web development, cloud integration, testing",
                EmailSubject = "Resume - Software Engineer Application",
                EmailSender = "\"Arjun Reddy\" <arjun.reddy@modernapps.in>",
                EmailDate = DateTime.Now.AddDays(-7),
                Source = "Database"
            },
            new {
                Id = Guid.NewGuid().ToString(),
                FileName = "neha_gupta_resume.docx",
                FilePath = "/sample/resumes/neha_gupta_resume.docx",
                Content = @"Neha Gupta
Senior Backend Developer
Contact Information:
Email: neha.gupta@backend.in
Phone: +91-98765-43210
6 years experience in .NET, C#, SQL Server
Skills: .NET, C#, SQL Server, Entity Framework, Azure, Docker
Experience: Microservices, database optimization, performance tuning",
                EmailSubject = "Application - Senior Backend Developer",
                EmailSender = "\"Neha Gupta\" <neha.gupta@backend.in>",
                EmailDate = DateTime.Now.AddDays(-8),
                Source = "Database"
            },
            new {
                Id = Guid.NewGuid().ToString(),
                FileName = "rohit_kumar_resume.pdf",
                FilePath = "/sample/resumes/rohit_kumar_resume.pdf",
                Content = @"Rohit Kumar
.NET Developer
Email: rohit.kumar@legacy.in
Phone: 011 2345 6789
Website: rohitkumar.net
4 years experience in C#, ASP.NET, SQL Server
Skills: C#, ASP.NET, SQL Server, JavaScript, jQuery, Bootstrap
Experience: Web development, legacy system maintenance, user support",
                EmailSubject = "Resume - .NET Developer Position",
                EmailSender = "\"Rohit Kumar\" <rohit.kumar@legacy.in>",
                EmailDate = DateTime.Now.AddDays(-9),
                Source = "Database"
            },
            new {
                Id = Guid.NewGuid().ToString(),
                FileName = "divya_sharma_resume.pdf",
                FilePath = "/sample/resumes/divya_sharma_resume.pdf",
                Content = @"Divya Sharma
Full Stack .NET Developer
Contact: divya.sharma@fullstack.in
Phone: 9876543210
GitHub: github.com/divyasharma
5 years experience in .NET, C#, React
Skills: .NET, C#, React, TypeScript, SQL Server, Azure
Experience: Modern web applications, responsive design, API development",
                EmailSubject = "Application for Full Stack .NET Developer",
                EmailSender = "\"Divya Sharma\" <divya.sharma@fullstack.in>",
                EmailDate = DateTime.Now.AddDays(-10),
                Source = "Database"
            },
            new {
                Id = Guid.NewGuid().ToString(),
                FileName = "amit_kumar_resume.docx",
                FilePath = "/sample/resumes/amit_kumar_resume.docx",
                Content = @"Amit Kumar
Senior Software Engineer
Email: amit.kumar@senior.in
Phone: +91 98765 43210
LinkedIn: linkedin.com/in/amitkumar
8 years experience in .NET, C#, Azure
Skills: .NET, C#, Azure, SQL Server, Docker, Kubernetes, CI/CD
Experience: Cloud architecture, DevOps, team mentoring, project management",
                EmailSubject = "Resume - Senior Software Engineer Role",
                EmailSender = "\"Amit Kumar\" <amit.kumar@senior.in>",
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
            INSERT INTO resumes (id, fileName, filePath, content, emailSubject, emailSender, email, phone, emailDate, source, createdAt, processedAt, status)
            VALUES (@id, @fileName, @filePath, @content, @emailSubject, @emailSender, @email, @phone, @emailDate, @source, @createdAt, @processedAt, @status)";

        int count = 0;
        foreach (var resume in sampleResumes)
        {
            // Extract email from content
            var extractedEmail = ExtractEmailFromContent(resume.Content);
            
            // Extract phone number from content
            var extractedPhone = ExtractPhoneFromContent(resume.Content);
            
            using var insertCommand = new SQLiteCommand(insertSql, connection);
            insertCommand.Parameters.AddWithValue("@id", resume.Id);
            insertCommand.Parameters.AddWithValue("@fileName", resume.FileName);
            insertCommand.Parameters.AddWithValue("@filePath", resume.FilePath);
            insertCommand.Parameters.AddWithValue("@content", resume.Content);
            insertCommand.Parameters.AddWithValue("@emailSubject", resume.EmailSubject);
            insertCommand.Parameters.AddWithValue("@emailSender", resume.EmailSender);
            insertCommand.Parameters.AddWithValue("@email", extractedEmail ?? (object)DBNull.Value);
            insertCommand.Parameters.AddWithValue("@phone", extractedPhone ?? (object)DBNull.Value);
            insertCommand.Parameters.AddWithValue("@emailDate", resume.EmailDate.ToString("yyyy-MM-dd HH:mm:ss"));
            insertCommand.Parameters.AddWithValue("@source", resume.Source);
            insertCommand.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            insertCommand.Parameters.AddWithValue("@processedAt", (object)DBNull.Value);
            insertCommand.Parameters.AddWithValue("@status", 0);

            insertCommand.ExecuteNonQuery();
            count++;
            Console.WriteLine($"Inserted resume {count}: {resume.FileName} (Email: {extractedEmail ?? "Not found"}, Phone: {extractedPhone ?? "Not found"})");
        }

        // Verify the data
        using var countCommand = new SQLiteCommand("SELECT COUNT(*) FROM resumes", connection);
        var totalCount = countCommand.ExecuteScalar();
        Console.WriteLine($"\nDatabase created successfully with {totalCount} resumes.");

        // Show sample data with extracted emails
        Console.WriteLine("\nSample data with extracted emails:");
        using var selectCommand = new SQLiteCommand("SELECT id, fileName, emailSender, email, phone, source FROM resumes LIMIT 5", connection);
        using var reader = selectCommand.ExecuteReader();
        while (reader.Read())
        {
            var extractedEmail = reader["email"]?.ToString() ?? "Not found";
            var extractedPhone = reader["phone"]?.ToString() ?? "Not found";
            Console.WriteLine($"ID: {reader["id"]}, File: {reader["fileName"]}, Sender: {reader["emailSender"]}, Extracted Email: {extractedEmail}, Extracted Phone: {extractedPhone}, Source: {reader["source"]}");
        }

        connection.Close();
        Console.WriteLine("\nDatabase creation completed!");
    }
} 