import re
import json
import os
from typing import List, Dict, Any, Optional, Tuple
from collections import Counter
import nltk
from nltk.corpus import stopwords
from nltk.tokenize import word_tokenize
import PyPDF2
from docx import Document

# Download required NLTK data
try:
    nltk.data.find('tokenizers/punkt')
except LookupError:
    nltk.download('punkt')

try:
    nltk.data.find('corpora/stopwords')
except LookupError:
    nltk.download('stopwords')

class ResumeParser:
    def __init__(self):
        self.stop_words = set(stopwords.words('english'))
        # Add common resume stop words
        self.stop_words.update(['experience', 'years', 'skills', 'education', 'work', 'job', 'position'])
    
    def extract_text_from_file(self, file_path: str) -> str:
        """Extract text from PDF or DOCX file"""
        try:
            if file_path.lower().endswith('.pdf'):
                return self.extract_text_from_pdf(file_path)
            elif file_path.lower().endswith('.docx') or file_path.lower().endswith('.doc'):
                return self.extract_text_from_docx(file_path)
            else:
                return ""
        except Exception as e:
            print(f"Error extracting text from {file_path}: {e}")
            return ""
    
    def extract_text_from_pdf(self, file_path: str) -> str:
        """Extract text from PDF file"""
        try:
            with open(file_path, 'rb') as file:
                pdf_reader = PyPDF2.PdfReader(file)
                text = ""
                for page in pdf_reader.pages:
                    text += page.extract_text() + "\n"
                return text
        except Exception as e:
            print(f"Error reading PDF {file_path}: {e}")
            return ""
    
    def extract_text_from_docx(self, file_path: str) -> str:
        """Extract text from DOCX file"""
        try:
            doc = Document(file_path)
            text = ""
            for paragraph in doc.paragraphs:
                text += paragraph.text + "\n"
            return text
        except Exception as e:
            print(f"Error reading DOCX {file_path}: {e}")
            return ""
    
    def extract_keywords_from_job_description(self, job_description: str) -> List[str]:
        """Extract important keywords from job description"""
        # Convert to lowercase and tokenize
        tokens = word_tokenize(job_description.lower())
        
        # Remove stop words and non-alphabetic tokens
        keywords = []
        for token in tokens:
            if token.isalpha() and token not in self.stop_words and len(token) > 2:
                keywords.append(token)
        
        # Count frequency and return most common keywords
        keyword_counts = Counter(keywords)
        return [keyword for keyword, count in keyword_counts.most_common(20)]
    
    def calculate_keyword_matches(self, resume_text: str, keywords: List[str]) -> List[Dict[str, Any]]:
        """Calculate keyword matches in resume text"""
        resume_lower = resume_text.lower()
        matches = []
        
        for keyword in keywords:
            # Count occurrences
            count = resume_lower.count(keyword.lower())
            
            if count > 0:
                # Find context around keyword
                # context = self.find_keyword_context(resume_text, keyword)
                
                # Calculate weight (fixed weight of 0.1 per keyword)
                # weight = self.calculate_keyword_weight(keyword, count)
                weight = 0.1
                
                matches.append({
                    'keyword': keyword,
                    # 'count': count,
                    'weight': weight,
                    # 'context': context
                })
        
        return matches
    
    def find_keyword_context(self, text: str, keyword: str, context_length: int = 50) -> List[str]:
        """Find context around keyword occurrences"""
        contexts = []
        text_lower = text.lower()
        keyword_lower = keyword.lower()
        
        start = 0
        while True:
            pos = text_lower.find(keyword_lower, start)
            if pos == -1:
                break
            
            # Extract context around keyword
            context_start = max(0, pos - context_length)
            context_end = min(len(text), pos + len(keyword) + context_length)
            context = text[context_start:context_end].strip()
            
            if context:
                contexts.append(context)
            
            start = pos + 1
        
        return contexts[:3]  # Limit to 3 contexts
    
    def calculate_keyword_weight(self, keyword: str, count: int) -> float:
        """Calculate weight for keyword based on importance and frequency"""
        # Base weight from frequency
        base_weight = min(count * 0.1, 1.0)
        
        # Boost for important technical keywords
        technical_keywords = [
            'python', 'java', 'c#', 'javascript', 'react', 'angular', 'vue',
            'sql', 'mysql', 'postgresql', 'mongodb', 'docker', 'kubernetes',
            'aws', 'azure', 'gcp', 'git', 'jenkins', 'agile', 'scrum'
        ]
        
        if keyword.lower() in technical_keywords:
            base_weight *= 1.5
        
        return min(base_weight, 1.0)
    
    def calculate_skills_match_percentage(self, resume_text: str, required_skills: List[str]) -> float:
        """Calculate percentage of required skills found in resume"""
        if not required_skills:
            return 0.0
        
        resume_lower = resume_text.lower()
        found_skills = 0
        
        for skill in required_skills:
            if skill.lower() in resume_lower:
                found_skills += 1
        
        return (found_skills / len(required_skills)) * 100
    
    def calculate_experience_match_percentage(self, resume_text: str, job_description: str) -> float:
        """Calculate experience match percentage based on years mentioned"""
        # Extract years of experience from resume
        resume_years = self.extract_years_of_experience(resume_text)
        job_years = self.extract_years_of_experience(job_description)
        
        if not resume_years or not job_years:
            return 50.0  # Default score if can't determine
        
        # Calculate match based on experience requirements
        min_required = min(job_years)
        max_required = max(job_years)
        resume_avg = sum(resume_years) / len(resume_years)
        
        if min_required <= resume_avg <= max_required:
            return 100.0
        elif resume_avg > max_required:
            return 80.0  # Overqualified
        else:
            return max(0, (resume_avg / max_required) * 100)
    
    def extract_years_of_experience(self, text: str) -> List[int]:
        """Extract years of experience from text"""
        years = []
        patterns = [
            r'(\d+)\s*years?\s*of?\s*experience',
            r'experience[:\s]*(\d+)\s*years?',
            r'(\d+)\s*years?\s*in\s*\w+'
        ]
        
        for pattern in patterns:
            matches = re.findall(pattern, text.lower())
            for match in matches:
                try:
                    years.append(int(match))
                except ValueError:
                    continue
        
        return years
    
    def rank_resume(self, resume_data: Dict[str, Any], job_description: str, 
                   required_skills: Optional[List[str]] = None) -> Dict[str, Any]:
        """Rank a resume against job description"""
        # Extract text from resume
        resume_text = resume_data.get('content', '')
        if not resume_text and resume_data.get('filePath'):
            resume_text = self.extract_text_from_file(resume_data['filePath'])
        
        if not resume_text:
            return {
                'resume': resume_data,
                'score': 0.0,
                'rank': 999,
                'keywordMatches': [],
                # 'skillsMatchPercentage': 0.0,
                # 'experienceMatchPercentage': 0.0,
                # 'summary': 'No text content found'
            }
        
        # Extract keywords from job description
        keywords = self.extract_keywords_from_job_description(job_description)
        
        # Calculate keyword matches
        keyword_matches = self.calculate_keyword_matches(resume_text, keywords)
        
        # Calculate scores
        keyword_score = sum(match['weight'] for match in keyword_matches)
        skills_score = self.calculate_skills_match_percentage(resume_text, required_skills or [])
        experience_score = self.calculate_experience_match_percentage(resume_text, job_description)
        
        # Calculate overall score (weighted average)
        overall_score = (keyword_score * 0.5 + skills_score * 0.3 + experience_score * 0.2)
        
        # Generate summary
        summary = self.generate_ranking_summary(keyword_matches, skills_score, experience_score)
        
        return {
            'resume': resume_data,
            'score': round(overall_score, 2),
            'rank': 0,  # Will be set by calling function
            'keywordMatches': keyword_matches,
            # 'skillsMatchPercentage': round(skills_score, 2),
            # 'experienceMatchPercentage': round(experience_score, 2),
            # 'summary': summary,
            'resumeSource': resume_data.get('source', 'Unknown')
        }
    
    def generate_ranking_summary(self, keyword_matches: List[Dict[str, Any]], 
                               skills_score: float, experience_score: float) -> str:
        """Generate a summary of the ranking"""
        total_keywords = len(keyword_matches)
        top_keywords = [match['keyword'] for match in sorted(keyword_matches, key=lambda x: x['weight'], reverse=True)[:5]]
        
        summary_parts = []
        
        if total_keywords > 0:
            summary_parts.append(f"Matched {total_keywords} keywords including {', '.join(top_keywords)}")
        
        if skills_score > 0:
            summary_parts.append(f"Skills match: {skills_score:.1f}%")
        
        if experience_score > 0:
            summary_parts.append(f"Experience match: {experience_score:.1f}%")
        
        if not summary_parts:
            return "No significant matches found"
        
        return ". ".join(summary_parts) + "."
    
    def rank_resumes(self, resumes: List[Dict[str, Any]], job_description: str,
                    required_skills: Optional[List[str]] = None) -> List[Dict[str, Any]]:
        """Rank multiple resumes against job description"""
        rankings = []
        
        for resume in resumes:
            ranking = self.rank_resume(resume, job_description, required_skills)
            rankings.append(ranking)
        
        # Sort by score descending, then by candidate name (emailSender) ascending for ties
        rankings.sort(key=lambda x: (-x['score'], x['resume'].get('emailSender', '')))
        
        for i, ranking in enumerate(rankings):
            ranking['rank'] = i + 1
        
        return rankings

if __name__ == "__main__":
    import sys
    
    parser = ResumeParser()
    
    if len(sys.argv) < 2:
        # Default behavior - process all resumes in temp_resumes directory
        job_desc = """
        Senior Software Engineer
        We are looking for a Senior Software Engineer with 5+ years of experience in C#, .NET, and SQL Server.
        Required skills: C#, .NET, SQL Server, JavaScript, React
        Preferred skills: Azure, Docker, Git
        """
        required_skills = ["C#", ".NET", "SQL Server", "JavaScript", "React"]

        # Directory containing resumes
        resumes_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "temp_resumes")
        resume_files = [f for f in os.listdir(resumes_dir) if f.lower().endswith((".pdf", ".docx", ".doc"))]

        resumes = []
        for idx, file_name in enumerate(resume_files, 1):
            file_path = os.path.join(resumes_dir, file_name)
            content = parser.extract_text_from_file(file_path)
            resumes.append({
                'id': f'file_{idx}',
                'fileName': file_name,
                'filePath': file_path,
                'content': content,
                'source': 'Email'
            })

        rankings = parser.rank_resumes(resumes, job_desc, required_skills)
        print(json.dumps(rankings, indent=2))
    
    elif sys.argv[1] == "extract_text" and len(sys.argv) > 2:
        # Extract text from a specific file
        file_path = sys.argv[2]
        text = parser.extract_text_from_file(file_path)
        print(text)
    
    elif sys.argv[1] == "extract_keywords" and len(sys.argv) > 2:
        # Extract keywords from job description
        job_description = sys.argv[2]
        keywords = parser.extract_keywords_from_job_description(job_description)
        print(json.dumps(keywords))
    
    elif sys.argv[1] == "rank_resume" and len(sys.argv) > 3:
        # Rank a single resume
        import json
        resume_data = json.loads(sys.argv[2])
        job_description = sys.argv[3]
        ranking = parser.rank_resume(resume_data, job_description)
        print(json.dumps(ranking))
    
    elif sys.argv[1] == "rank_resumes" and len(sys.argv) > 3:
        # Rank multiple resumes
        import json
        resumes = json.loads(sys.argv[2])
        job_description = sys.argv[3]
        rankings = parser.rank_resumes(resumes, job_description)
        print(json.dumps(rankings))
    
    else:
        print("Usage:")
        print("  python resume_parser.py                                    # Process all resumes in temp_resumes")
        print("  python resume_parser.py extract_text <file_path>          # Extract text from file")
        print("  python resume_parser.py extract_keywords <job_description> # Extract keywords")
        print("  python resume_parser.py rank_resume <resume_json> <job_description> # Rank single resume")
        print("  python resume_parser.py rank_resumes <resumes_json> <job_description> # Rank multiple resumes") 