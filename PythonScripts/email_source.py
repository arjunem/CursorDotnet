import imaplib
import email.message
import os
import json
import re
from email.header import decode_header
from datetime import datetime
import base64
import tempfile
import shutil
from typing import Optional, List, Dict, Any
import sys

class EmailSource:
    def __init__(self, email_config: Optional[Dict[str, Any]] = None):
        if email_config is None:
            email_config = self.load_config()
        self.email_config = email_config
        
        # Create a specific temp folder in the project directory
        script_dir = os.path.dirname(os.path.abspath(__file__))
        self.temp_dir = os.path.join(script_dir, "temp_resumes")
        
        # Create the temp directory if it doesn't exist
        if not os.path.exists(self.temp_dir):
            os.makedirs(self.temp_dir)
            print(f"Created temp directory: {self.temp_dir}")
        
        print(f"Using temp directory: {self.temp_dir}")
        
    def load_config(self) -> Dict[str, Any]:
        """Load email configuration from file"""
        config_path = os.path.join(os.path.dirname(__file__), 'email_config.json')
        try:
            with open(config_path, 'r') as f:
                return json.load(f)
        except FileNotFoundError:
            print(f"Config file not found: {config_path}")
            return {
                'email': 'your-email@gmail.com',
                'password': 'your-app-password',
                'imap_server': 'imap.gmail.com',
                'imap_port': 993,
                'subject_filter': 'resume',
                'attachment_extensions': ['.pdf', '.docx', '.doc']
            }
        except json.JSONDecodeError as e:
            print(f"Error parsing config file: {e}")
            return {}
        
    def connect_to_email(self) -> Optional[imaplib.IMAP4_SSL]:
        """Connect to email server using IMAP"""
        try:
            if not self.email_config.get('email') or not self.email_config.get('password'):
                print("Email credentials not configured. Please update email_config.json")
                return None
                
            mail = imaplib.IMAP4_SSL(self.email_config.get('imap_server', 'imap.gmail.com'))
            mail.login(self.email_config['email'], self.email_config['password'])
            mail.select('INBOX')
            return mail
        except Exception as e:
            print(f"Error connecting to email: {e}")
            return None
    
    def fetch_resumes_from_email(self, subject_filter: str = "resume", attachment_extensions: Optional[List[str]] = None) -> List[Dict[str, Any]]:
        """Fetch resumes from email with specified subject filter and attachment types"""
        if attachment_extensions is None:
            attachment_extensions = self.email_config.get('attachment_extensions', ['.pdf', '.docx', '.doc'])
            
        mail = self.connect_to_email()
        if not mail:
            return []
            
        resumes = []
        
        try:
            # Search for emails with resume in subject
            search_criteria = f'SUBJECT "{subject_filter}"'
            status, messages = mail.search(None, search_criteria)
            
            if status != 'OK' or not messages or not messages[0]:
                return []
                
            # Ensure messages[0] is not None before splitting
            if messages[0] is None:
                return []
                
            email_ids = messages[0].split()
            
            # Handle case where messages[0] might be None
            if not email_ids:
                return []
                
            for email_id in email_ids:
                try:
                    status, msg_data = mail.fetch(email_id, '(RFC822)')
                    if status != 'OK' or not msg_data or not msg_data[0]:
                        continue
                        
                    email_body = msg_data[0][1]
                    if not isinstance(email_body, bytes):
                        continue
                        
                    email_message = email.message_from_bytes(email_body)
                    
                    # Extract email metadata
                    subject = self.decode_email_header(email_message['subject'])
                    sender = self.decode_email_header(email_message['from'])
                    date = email_message['date']
                    
                    # Process attachments
                    for part in email_message.walk():
                        if part.get_content_maintype() == 'multipart':
                            continue
                            
                        filename = part.get_filename()
                        if filename:
                            filename = self.decode_email_header(filename)
                            
                            # Check if attachment is a resume file
                            if any(filename.lower().endswith(ext) for ext in attachment_extensions):
                                # Save attachment
                                file_path = self.save_attachment(part, filename)
                                
                                if file_path:
                                    resume_data = {
                                        'id': str(len(resumes) + 1),
                                        'fileName': filename,
                                        'filePath': file_path,
                                        'emailSubject': subject,
                                        'emailSender': sender,
                                        'emailDate': date,
                                        'source': 'Email',
                                        'createdAt': datetime.now().isoformat(),
                                        'status': 'Pending'
                                    }
                                    resumes.append(resume_data)
                                    
                except Exception as e:
                    print(f"Error processing email {email_id}: {e}")
                    continue
                    
        except Exception as e:
            print(f"Error fetching emails: {e}")
        finally:
            try:
                mail.logout()
            except:
                pass
                
        return resumes
    
    def decode_email_header(self, header: Optional[str]) -> str:
        """Decode email header properly"""
        if header is None:
            return ""
            
        decoded_parts = decode_header(header)
        decoded_string = ""
        
        for part, encoding in decoded_parts:
            if isinstance(part, bytes):
                if encoding:
                    decoded_string += part.decode(encoding)
                else:
                    decoded_string += part.decode('utf-8', errors='ignore')
            else:
                decoded_string += str(part)
                
        return decoded_string
    
    def save_attachment(self, part: email.message.Message, filename: str) -> Optional[str]:
        """Save email attachment to temp directory"""
        try:
            # Clean filename to avoid path issues
            safe_filename = "".join(c for c in filename if c.isalnum() or c in (' ', '-', '_', '.')).rstrip()
            file_path = os.path.join(self.temp_dir, safe_filename)
            
            payload = part.get_payload(decode=True)
            if payload is None:
                return None
                
            # Ensure payload is bytes
            if isinstance(payload, bytes):
                with open(file_path, 'wb') as f:
                    f.write(payload)
            else:
                # Handle other payload types
                with open(file_path, 'wb') as f:
                    f.write(str(payload).encode('utf-8'))
                
            print(f"Saved attachment: {safe_filename}")
            return file_path
        except Exception as e:
            print(f"Error saving attachment {filename}: {e}")
            return None
    
    def cleanup(self) -> None:
        """Clean up temporary files"""
        try:
            shutil.rmtree(self.temp_dir)
        except:
            pass
    
    def list_downloaded_files(self) -> List[str]:
        """List all downloaded files in the temp directory"""
        if not os.path.exists(self.temp_dir):
            return []
        
        files = []
        for filename in os.listdir(self.temp_dir):
            file_path = os.path.join(self.temp_dir, filename)
            if os.path.isfile(file_path):
                files.append(filename)
        return files

def test_email_connection():
    """Test email connection with current configuration"""
    email_source = EmailSource()
    
    print("Testing email connection...")
    print(f"Email: {email_source.email_config.get('email', 'Not configured')}")
    print(f"Server: {email_source.email_config.get('imap_server', 'Not configured')}")
    
    mail = email_source.connect_to_email()
    if mail:
        print("✅ Email connection successful!")
        mail.logout()
    else:
        print("❌ Email connection failed!")
        print("\nTo configure email access:")
        print("1. Update email_config.json with your credentials")
        print("2. For Gmail, use an App Password instead of your regular password")
        print("3. Enable 2-factor authentication and generate an App Password")

if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == "test":
        test_email_connection()
    elif len(sys.argv) > 1 and sys.argv[1] == "list":
        email_source = EmailSource()
        files = email_source.list_downloaded_files()
        print(f"Downloaded files in {email_source.temp_dir}:")
        for file in files:
            print(f"  - {file}")
    else:
        # Default behavior - fetch resumes and output JSON
        email_source = EmailSource()
        resumes = email_source.fetch_resumes_from_email()
        
        # Convert to the format expected by .NET services
        formatted_resumes = []
        for resume in resumes:
            if resume is not None:  # Add null check
                formatted_resume = {
                    'id': resume['id'],
                    'fileName': resume['fileName'],
                    'filePath': resume['filePath'],
                    'content': '',  # Will be extracted by parsing service
                    'source': resume['source'],
                    'createdAt': resume['createdAt'],
                    'status': resume['status']
                }
                formatted_resumes.append(formatted_resume)
        
        print(json.dumps(formatted_resumes, indent=2)) 