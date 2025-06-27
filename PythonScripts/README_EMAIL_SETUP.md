# Email Setup Instructions

## Gmail Setup (Recommended)

### 1. Enable 2-Factor Authentication
1. Go to your Google Account settings: https://myaccount.google.com/
2. Navigate to "Security" → "2-Step Verification"
3. Enable 2-Step Verification if not already enabled

### 2. Generate an App Password
1. In the same Security section, go to "App passwords"
2. Select "Mail" as the app and "Other" as the device
3. Click "Generate"
4. Copy the 16-character password (it will look like: xxxx xxxx xxxx xxxx)

### 3. Update Configuration
Edit `email_config.json` and replace the placeholder values:

```json
{
  "email": "your-actual-email@gmail.com",
  "password": "your-16-character-app-password",
  "imap_server": "imap.gmail.com",
  "imap_port": 993,
  "subject_filter": "resume",
  "attachment_extensions": [".pdf", ".docx", ".doc"]
}
```

### 4. Test the Connection
Run the test command:
```bash
python email_source.py test
```

## Other Email Providers

### Outlook/Hotmail
```json
{
  "email": "your-email@outlook.com",
  "password": "your-app-password",
  "imap_server": "outlook.office365.com",
  "imap_port": 993
}
```

### Yahoo Mail
```json
{
  "email": "your-email@yahoo.com",
  "password": "your-app-password",
  "imap_server": "imap.mail.yahoo.com",
  "imap_port": 993
}
```

## Security Notes

1. **Never commit real credentials to version control**
2. **Use App Passwords instead of your main password**
3. **Consider using environment variables for production**
4. **The config file should be added to .gitignore**

## Troubleshooting

### "Invalid credentials" error
- Make sure you're using an App Password, not your regular password
- Ensure 2-Factor Authentication is enabled
- Check that the email address is correct

### "Connection refused" error
- Check your internet connection
- Verify the IMAP server and port are correct
- Some networks may block IMAP connections

### "SSL certificate" error
- This is usually a network/proxy issue
- Try using a different network connection 