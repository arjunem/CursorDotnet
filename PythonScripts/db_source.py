import sqlite3
import json
import os
from datetime import datetime
from typing import List, Dict, Any, Optional

class DBSource:
    def __init__(self, db_path: str = "resumes.db"):
        self.db_path = db_path
        self.init_database()
    
    def init_database(self) -> None:
        """Initialize the database with required tables"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            # Create resumes table
            cursor.execute('''
                CREATE TABLE IF NOT EXISTS resumes (
                    id TEXT PRIMARY KEY,
                    fileName TEXT NOT NULL,
                    filePath TEXT NOT NULL,
                    content TEXT,
                    source TEXT DEFAULT 'Database',
                    createdAt TEXT NOT NULL,
                    processedAt TEXT,
                    status TEXT DEFAULT 'Pending'
                )
            ''')
            
            # Create sample data if table is empty
            cursor.execute("SELECT COUNT(*) FROM resumes")
            if cursor.fetchone()[0] == 0:
                self.insert_sample_data(cursor)
            
            conn.commit()
            conn.close()
            
        except Exception as e:
            print(f"Error initializing database: {e}")
    
    def insert_sample_data(self, cursor: sqlite3.Cursor) -> None:
        """Insert sample resume data for testing"""
        sample_resumes = [
            {
                'id': 'db_001',
                'fileName': 'john_doe_resume.pdf',
                'filePath': '/sample/resumes/john_doe_resume.pdf',
                'content': 'John Doe\nSoftware Engineer\n5 years experience in C#, .NET, SQL Server\nSkills: C#, .NET, SQL Server, JavaScript, React',
                'source': 'Database',
                'createdAt': datetime.now().isoformat(),
                'status': 'Pending'
            },
            {
                'id': 'db_002',
                'fileName': 'jane_smith_resume.docx',
                'filePath': '/sample/resumes/jane_smith_resume.docx',
                'content': 'Jane Smith\nSenior Developer\n8 years experience in Python, Django, PostgreSQL\nSkills: Python, Django, PostgreSQL, Docker, AWS',
                'source': 'Database',
                'createdAt': datetime.now().isoformat(),
                'status': 'Pending'
            },
            {
                'id': 'db_003',
                'fileName': 'mike_johnson_resume.pdf',
                'filePath': '/sample/resumes/mike_johnson_resume.pdf',
                'content': 'Mike Johnson\nFull Stack Developer\n6 years experience in Java, Spring, MySQL\nSkills: Java, Spring, MySQL, Angular, Git',
                'source': 'Database',
                'createdAt': datetime.now().isoformat(),
                'status': 'Pending'
            }
        ]
        
        for resume in sample_resumes:
            cursor.execute('''
                INSERT INTO resumes (id, fileName, filePath, content, source, createdAt, status)
                VALUES (?, ?, ?, ?, ?, ?, ?)
            ''', (
                resume['id'],
                resume['fileName'],
                resume['filePath'],
                resume['content'],
                resume['source'],
                resume['createdAt'],
                resume['status']
            ))
    
    def fetch_resumes_from_database(self) -> List[Dict[str, Any]]:
        """Fetch all resumes from the database"""
        resumes = []
        
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                SELECT id, fileName, filePath, content, source, createdAt, processedAt, status
                FROM resumes
                ORDER BY createdAt DESC
            ''')
            
            rows = cursor.fetchall()
            
            for row in rows:
                resume_data = {
                    'id': row[0],
                    'fileName': row[1],
                    'filePath': row[2],
                    'content': row[3],
                    'source': row[4],
                    'createdAt': row[5],
                    'processedAt': row[6],
                    'status': row[7]
                }
                resumes.append(resume_data)
            
            conn.close()
            
        except Exception as e:
            print(f"Error fetching resumes from database: {e}")
        
        return resumes
    
    def add_resume_to_database(self, resume_data: Dict[str, Any]) -> bool:
        """Add a new resume to the database"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT INTO resumes (id, fileName, filePath, content, source, createdAt, status)
                VALUES (?, ?, ?, ?, ?, ?, ?)
            ''', (
                resume_data.get('id'),
                resume_data.get('fileName'),
                resume_data.get('filePath'),
                resume_data.get('content'),
                resume_data.get('source', 'Database'),
                resume_data.get('createdAt', datetime.now().isoformat()),
                resume_data.get('status', 'Pending')
            ))
            
            conn.commit()
            conn.close()
            return True
            
        except Exception as e:
            print(f"Error adding resume to database: {e}")
            return False
    
    def update_resume_status(self, resume_id: str, status: str, content: Optional[str] = None) -> bool:
        """Update resume status and optionally content"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            if content:
                cursor.execute('''
                    UPDATE resumes 
                    SET status = ?, processedAt = ?, content = ?
                    WHERE id = ?
                ''', (status, datetime.now().isoformat(), content, resume_id))
            else:
                cursor.execute('''
                    UPDATE resumes 
                    SET status = ?, processedAt = ?
                    WHERE id = ?
                ''', (status, datetime.now().isoformat(), resume_id))
            
            conn.commit()
            conn.close()
            return True
            
        except Exception as e:
            print(f"Error updating resume status: {e}")
            return False

if __name__ == "__main__":
    # Example usage
    db_source = DBSource()
    resumes = db_source.fetch_resumes_from_database()
    print(json.dumps(resumes, indent=2)) 