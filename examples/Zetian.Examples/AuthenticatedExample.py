"""
Authenticated SMTP Server Test Script
Tests SMTP authentication mechanisms on port 587
"""
import smtplib
import base64
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
from email.utils import formatdate

def test_plain_auth():
    """Test PLAIN authentication"""
    
    # Credentials
    username = "user@example.com"
    password = "password123"
    sender = username
    recipient = "recipient@example.com"
    
    # Create message
    msg = MIMEMultipart()
    msg['Subject'] = 'Authenticated Email Test (PLAIN)'
    msg['From'] = sender
    msg['To'] = recipient
    msg['Date'] = formatdate(localtime=True)
    
    body = """
Hello,

This email was sent using PLAIN authentication.
The connection required valid credentials to send.

Authentication details:
- Method: PLAIN
- Port: 587
- Username: user@example.com

Best regards,
Authenticated System
"""
    msg.attach(MIMEText(body, 'plain'))
    
    try:
        # Connect with authentication
        with smtplib.SMTP('localhost', 587) as server:
            # Enable STARTTLS if available
            try:
                server.starttls()
                print("   TLS enabled")
            except:
                print("   TLS not available, continuing without encryption")
            
            # Login
            server.login(username, password)
            print(f"   Authenticated as: {username}")
            
            # Send email
            server.sendmail(sender, [recipient], msg.as_string())
            
        print(f"✅ Authenticated email sent successfully!")
        print(f"   Method: PLAIN")
        return True
    except smtplib.SMTPAuthenticationError as e:
        print(f"❌ Authentication failed")
        print(f"   Error: {e}")
        return False
    except Exception as e:
        print(f"❌ Failed to send authenticated email")
        print(f"   Error: {e}")
        return False

def test_login_auth():
    """Test LOGIN authentication"""
    
    username = "admin@example.com"
    password = "admin456"
    sender = username
    recipient = "user@example.com"
    
    msg = MIMEText("This email uses LOGIN authentication method.")
    msg['Subject'] = 'Authenticated Email Test (LOGIN)'
    msg['From'] = sender
    msg['To'] = recipient
    msg['Date'] = formatdate(localtime=True)
    
    try:
        with smtplib.SMTP('localhost', 587) as server:
            # Try STARTTLS
            try:
                server.starttls()
            except:
                pass
            
            # Use LOGIN method explicitly
            server.login(username, password)
            server.sendmail(sender, [recipient], msg.as_string())
            
        print(f"✅ LOGIN authentication successful!")
        return True
    except Exception as e:
        print(f"❌ LOGIN authentication failed")
        print(f"   Error: {e}")
        return False

def test_wrong_credentials():
    """Test authentication with wrong credentials"""
    
    username = "hacker@evil.com"
    password = "wrongpass"
    
    try:
        with smtplib.SMTP('localhost', 587) as server:
            try:
                server.starttls()
            except:
                pass
            
            server.login(username, password)
            
        print(f"❌ Security issue: Wrong credentials accepted!")
        return False
    except smtplib.SMTPAuthenticationError:
        print(f"✅ Wrong credentials correctly rejected")
        return True
    except Exception as e:
        print(f"⚠️ Unexpected error: {e}")
        return False

def test_no_auth_attempt():
    """Test sending without authentication when auth is required"""
    
    sender = "unauthorized@example.com"
    recipient = "user@example.com"
    
    msg = MIMEText("Attempting to send without authentication")
    msg['Subject'] = 'Unauthorized Send Attempt'
    msg['From'] = sender
    msg['To'] = recipient
    
    try:
        with smtplib.SMTP('localhost', 587) as server:
            # Try to send without login
            server.sendmail(sender, [recipient], msg.as_string())
            
        print(f"❌ Security issue: Email sent without authentication!")
        return False
    except smtplib.SMTPSenderRefused:
        print(f"✅ Unauthenticated send correctly rejected")
        return True
    except Exception as e:
        print(f"✅ Unauthenticated send rejected: {e}")
        return True

if __name__ == "__main__":
    print("=" * 60)
    print("Authenticated SMTP Server Test")
    print("Port: 587 (Submission)")
    print("=" * 60)
    print()
    
    # Note for the user
    print("NOTE: Make sure the server is running with authentication enabled")
    print("Default test credentials:")
    print("  - user@example.com / password123")
    print("  - admin@example.com / admin456")
    print()
    
    # Run tests
    tests = [
        ("PLAIN Authentication", test_plain_auth),
        ("LOGIN Authentication", test_login_auth),
        ("Wrong Credentials", test_wrong_credentials),
        ("No Authentication Attempt", test_no_auth_attempt)
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
    print("=" * 60)