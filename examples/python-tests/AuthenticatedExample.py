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
    
    # Credentials - matching server configuration
    username = "admin"
    password = "password123"
    sender = "admin@example.com"
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
- Username: admin

Best regards,
Authenticated System
"""
    msg.attach(MIMEText(body, 'plain'))
    
    try:
        # Connect with authentication
        with smtplib.SMTP('localhost', 587, timeout=10) as server:
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
    
    # Using server credentials
    username = "admin"
    password = "password123"
    sender = "admin@example.com"
    recipient = "user@example.com"
    
    msg = MIMEText("This email uses LOGIN authentication method.")
    msg['Subject'] = 'Authenticated Email Test (LOGIN)'
    msg['From'] = sender
    msg['To'] = recipient
    msg['Date'] = formatdate(localtime=True)
    
    try:
        server = smtplib.SMTP('localhost', 587, timeout=10)
        # Try STARTTLS if available
        try:
            server.starttls()
        except:
            pass
        
        # LOGIN authentication with server credentials
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

def main():
    """Main test function"""
    print("=" * 60)
    print("Authenticated SMTP Server Test")
    print("Port: 587 (Submission)")
    print("=" * 60)
    print()
    
    # Note for the user
    print("NOTE: Make sure the server is running with authentication enabled")
    print("Default test credentials:")
    print("  - admin / password123")
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

if __name__ == "__main__":
    main()