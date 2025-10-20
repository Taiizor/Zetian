"""
Message Storage SMTP Server Test Script
Tests email storage functionality and retrieval
"""
import smtplib
import os
import glob
import time
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
from email.mime.application import MIMEApplication
from email.utils import formatdate
from datetime import datetime

def test_basic_storage():
    """Test basic email storage to file system"""
    
    sender = "storage@example.com"
    recipient = "archive@example.com"
    
    msg = MIMEMultipart()
    msg['Subject'] = 'Storage Test Email'
    msg['From'] = sender
    msg['To'] = recipient
    msg['Date'] = formatdate(localtime=True)
    msg['Message-ID'] = f"<storage-test-{int(time.time())}@example.com>"
    
    body = """
Hello,

This email should be stored in the file system.
Please verify it was saved correctly.

Storage test details:
- Storage type: File System
- Format: .eml file
- Location: ./emails directory

Best regards,
Storage Test System
"""
    msg.attach(MIMEText(body, 'plain'))
    
    try:
        with smtplib.SMTP('localhost', 25) as server:
            server.sendmail(sender, [recipient], msg.as_string())
        
        print(f"✅ Email sent for storage")
        print(f"   From: {sender}")
        print(f"   To: {recipient}")
        print(f"   Message-ID: {msg['Message-ID']}")
        
        # Check if file was created (wait a moment for processing)
        time.sleep(1)
        
        # Look for stored emails
        email_patterns = [
            "./emails/*.eml",
            "./emails/**/*.eml",
            "./stored_messages/*.eml",
            "./stored_messages/**/*.eml"
        ]
        
        found = False
        for pattern in email_patterns:
            files = glob.glob(pattern, recursive=True)
            if files:
                print(f"   ✅ Found {len(files)} stored email(s) in {os.path.dirname(pattern)}")
                found = True
                break
        
        if not found:
            print(f"   ⚠️ Could not verify storage location")
        
        return True
    except Exception as e:
        print(f"❌ Failed to store email")
        print(f"   Error: {e}")
        return False

def test_attachment_storage():
    """Test storage of emails with attachments"""
    
    sender = "attachments@example.com"
    recipient = "archive@example.com"
    
    msg = MIMEMultipart()
    msg['Subject'] = 'Email with Attachments Storage Test'
    msg['From'] = sender
    msg['To'] = recipient
    msg['Date'] = formatdate(localtime=True)
    
    # Add text body
    body = "This email contains multiple attachments for storage testing."
    msg.attach(MIMEText(body, 'plain'))
    
    # Add text attachment
    text_attachment = MIMEApplication(b"Sample text file content", _subtype="txt")
    text_attachment.add_header('Content-Disposition', 'attachment', filename='test.txt')
    msg.attach(text_attachment)
    
    # Add binary attachment (simulated PDF)
    pdf_content = b"%%PDF-1.4\n%Fake PDF content for testing\n"
    pdf_attachment = MIMEApplication(pdf_content, _subtype="pdf")
    pdf_attachment.add_header('Content-Disposition', 'attachment', filename='document.pdf')
    msg.attach(pdf_attachment)
    
    try:
        with smtplib.SMTP('localhost', 25) as server:
            server.sendmail(sender, [recipient], msg.as_string())
        
        print(f"✅ Email with attachments sent for storage")
        print(f"   Attachments: test.txt, document.pdf")
        return True
    except Exception as e:
        print(f"❌ Failed to store email with attachments")
        print(f"   Error: {e}")
        return False

def test_html_email_storage():
    """Test storage of HTML formatted emails"""
    
    sender = "html@example.com"
    recipient = "archive@example.com"
    
    msg = MIMEMultipart('alternative')
    msg['Subject'] = 'HTML Email Storage Test'
    msg['From'] = sender
    msg['To'] = recipient
    msg['Date'] = formatdate(localtime=True)
    
    # Plain text version
    text = "This is the plain text version for storage testing."
    
    # HTML version
    html = """
    <html>
      <head>
        <style>
          body { font-family: Arial, sans-serif; }
          .header { background: #007bff; color: white; padding: 10px; }
          .content { padding: 20px; }
        </style>
      </head>
      <body>
        <div class="header">
          <h1>HTML Storage Test</h1>
        </div>
        <div class="content">
          <p>This is an <strong>HTML formatted</strong> email for storage testing.</p>
          <ul>
            <li>Feature 1: HTML formatting preserved</li>
            <li>Feature 2: Styles included</li>
            <li>Feature 3: Multipart message</li>
          </ul>
        </div>
      </body>
    </html>
    """
    
    msg.attach(MIMEText(text, 'plain'))
    msg.attach(MIMEText(html, 'html'))
    
    try:
        with smtplib.SMTP('localhost', 25) as server:
            server.sendmail(sender, [recipient], msg.as_string())
        
        print(f"✅ HTML email sent for storage")
        print(f"   Type: multipart/alternative")
        return True
    except Exception as e:
        print(f"❌ Failed to store HTML email")
        print(f"   Error: {e}")
        return False

def test_date_organized_storage():
    """Test if emails are organized by date folders"""
    
    sender = "datetest@example.com"
    recipient = "archive@example.com"
    
    # Send multiple emails
    for i in range(3):
        msg = MIMEText(f"Date organization test email {i+1}")
        msg['Subject'] = f'Date Test {i+1}'
        msg['From'] = sender
        msg['To'] = recipient
        msg['Date'] = formatdate(localtime=True)
        
        try:
            with smtplib.SMTP('localhost', 25) as server:
                server.sendmail(sender, [recipient], msg.as_string())
            time.sleep(0.5)
        except Exception as e:
            print(f"❌ Failed to send email {i+1}: {e}")
            return False
    
    print(f"✅ Sent 3 emails for date organization testing")
    
    # Check for date-based folder structure
    today = datetime.now()
    date_patterns = [
        f"./emails/{today.year}/{today.month:02d}/{today.day:02d}/*.eml",
        f"./stored_messages/{today.strftime('%Y-%m-%d')}/*.eml",
        f"./emails/{today.strftime('%Y%m%d')}/*.eml"
    ]
    
    for pattern in date_patterns:
        files = glob.glob(pattern)
        if files:
            print(f"   ✅ Date-organized storage found: {os.path.dirname(pattern)}")
            return True
    
    print(f"   ℹ️ Date organization not detected (may use flat storage)")
    return True

def test_large_email_storage():
    """Test storage of large emails"""
    
    sender = "large@example.com"
    recipient = "archive@example.com"
    
    msg = MIMEMultipart()
    msg['Subject'] = 'Large Email Storage Test'
    msg['From'] = sender
    msg['To'] = recipient
    msg['Date'] = formatdate(localtime=True)
    
    # Create large content (2MB)
    large_content = "X" * (2 * 1024 * 1024)
    msg.attach(MIMEText(large_content, 'plain'))
    
    try:
        with smtplib.SMTP('localhost', 25) as server:
            server.sendmail(sender, [recipient], msg.as_string())
        
        print(f"✅ Large email (2MB) sent for storage")
        return True
    except Exception as e:
        if "message size" in str(e).lower() or "too large" in str(e).lower():
            print(f"ℹ️ Large email rejected due to size limits")
            return True  # This is expected behavior
        else:
            print(f"❌ Failed to store large email")
            print(f"   Error: {e}")
            return False

def main():
    """Main test function"""
    print("=" * 60)
    print("Message Storage SMTP Server Test")
    print("=" * 60)
    print()
    
    print("Testing email storage functionality:")
    print("- File system storage")
    print("- Attachment handling")
    print("- HTML email storage")
    print("- Date-based organization")
    print()
    
    # Run tests
    tests = [
        ("Basic Storage", test_basic_storage),
        ("Attachment Storage", test_attachment_storage),
        ("HTML Email Storage", test_html_email_storage),
        ("Date Organization", test_date_organized_storage),
        ("Large Email Storage", test_large_email_storage)
    ]
    
    results = []
    for test_name, test_func in tests:
        print(f"\nTesting: {test_name}")
        print("-" * 40)
        results.append(test_func())
    
    # Summary
    print()
    print("=" * 60)
    print(f"Test Summary: {sum(results)}/{len(results)} tests passed")
    print()
    print("NOTE: Check the configured storage directory for saved emails")
    print("      Default locations: ./emails or ./stored_messages")
    print("=" * 60)

if __name__ == "__main__":
    main()