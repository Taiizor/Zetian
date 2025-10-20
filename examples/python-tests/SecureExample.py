"""
Secure SMTP Server Test Script (TLS/SSL)
Tests encrypted SMTP connections on port 465/587
"""
import smtplib
import ssl
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
from email.utils import formatdate

def test_starttls():
    """Test STARTTLS on port 587"""
    
    sender = "secure@example.com"
    recipient = "user@example.com"
    username = "secure@example.com"
    password = "securepass"
    
    msg = MIMEMultipart()
    msg['Subject'] = 'Secure Email Test (STARTTLS)'
    msg['From'] = sender
    msg['To'] = recipient
    msg['Date'] = formatdate(localtime=True)
    
    body = """
Hello,

This email was sent over a secure TLS connection.
The connection was upgraded using STARTTLS command.

Security details:
- Initial connection: Plain
- Upgraded to: TLS
- Port: 587
- Encryption: Active after STARTTLS

Best regards,
Secure System
"""
    msg.attach(MIMEText(body, 'plain'))
    
    try:
        # Connect and upgrade to TLS
        with smtplib.SMTP('localhost', 587) as server:
            print("   Connected on port 587")
            
            # Upgrade connection with STARTTLS
            context = ssl.create_default_context()
            context.check_hostname = False
            context.verify_mode = ssl.CERT_NONE  # For self-signed certificates
            
            server.starttls(context=context)
            print("   ✅ Connection upgraded to TLS")
            
            # Authenticate
            server.login(username, password)
            print(f"   ✅ Authenticated as: {username}")
            
            # Send email
            server.sendmail(sender, [recipient], msg.as_string())
            
        print(f"✅ Secure email sent via STARTTLS!")
        return True
    except Exception as e:
        print(f"❌ Failed to send secure email via STARTTLS")
        print(f"   Error: {e}")
        return False

def test_implicit_tls():
    """Test implicit TLS/SSL on port 465"""
    
    sender = "secure@example.com"
    recipient = "admin@example.com"
    username = "secure@example.com"
    password = "securepass"
    
    msg = MIMEText("This email was sent over implicit SSL/TLS connection.")
    msg['Subject'] = 'Secure Email Test (Implicit TLS)'
    msg['From'] = sender
    msg['To'] = recipient
    msg['Date'] = formatdate(localtime=True)
    
    try:
        # Create SSL context
        context = ssl.create_default_context()
        context.check_hostname = False
        context.verify_mode = ssl.CERT_NONE  # For self-signed certificates
        
        # Connect with SSL from the start
        with smtplib.SMTP_SSL('localhost', 465, context=context) as server:
            print("   Connected on port 465 with SSL")
            
            # Authenticate
            server.login(username, password)
            print(f"   ✅ Authenticated over SSL")
            
            # Send email
            server.sendmail(sender, [recipient], msg.as_string())
            
        print(f"✅ Secure email sent via implicit TLS!")
        return True
    except ConnectionRefusedError:
        print(f"⚠️ Port 465 not available (implicit TLS not configured)")
        return None  # Not a failure, just not configured
    except Exception as e:
        print(f"❌ Failed to send secure email via implicit TLS")
        print(f"   Error: {e}")
        return False

def test_plaintext_rejection():
    """Test that plaintext authentication is rejected when TLS is required"""
    
    username = "secure@example.com"
    password = "securepass"
    
    try:
        with smtplib.SMTP('localhost', 587) as server:
            # Try to authenticate without STARTTLS
            server.login(username, password)
            
        print(f"❌ Security issue: Plaintext auth accepted without TLS!")
        return False
    except smtplib.SMTPNotSupportedError:
        print(f"✅ Plaintext auth correctly rejected (TLS required)")
        return True
    except Exception as e:
        # Check if error message indicates encryption is required
        if "encryption required" in str(e).lower() or "538" in str(e):
            print(f"✅ Plaintext auth correctly rejected: {e}")
            return True
        else:
            print(f"⚠️ Unexpected error: {e}")
            return False

def test_cipher_info():
    """Test and display TLS cipher information"""
    
    try:
        with smtplib.SMTP('localhost', 587) as server:
            # Create SSL context
            context = ssl.create_default_context()
            context.check_hostname = False
            context.verify_mode = ssl.CERT_NONE
            
            # Upgrade to TLS
            server.starttls(context=context)
            
            # Get cipher info if available
            if hasattr(server.sock, 'cipher'):
                cipher = server.sock.cipher()
                print(f"✅ TLS Connection established")
                print(f"   Cipher suite: {cipher[0] if cipher else 'Unknown'}")
                print(f"   Protocol: {cipher[1] if cipher and len(cipher) > 1 else 'Unknown'}")
                print(f"   Key bits: {cipher[2] if cipher and len(cipher) > 2 else 'Unknown'}")
            else:
                print(f"✅ TLS Connection established (cipher info not available)")
            
            return True
    except Exception as e:
        print(f"❌ Failed to establish TLS connection")
        print(f"   Error: {e}")
        return False

def main():
    """Main test function"""
    print("=" * 60)
    print("Secure SMTP Server Test (TLS/SSL)")
    print("=" * 60)
    print()
    
    print("NOTE: Server should be configured with a certificate")
    print("Test will use self-signed certificate validation")
    print()
    
    # Run tests
    tests = [
        ("STARTTLS (Port 587)", test_starttls),
        ("Implicit TLS (Port 465)", test_implicit_tls),
        ("Plaintext Auth Rejection", test_plaintext_rejection),
        ("TLS Cipher Information", test_cipher_info)
    ]
    
    results = []
    for test_name, test_func in tests:
        print(f"\nTesting: {test_name}")
        print("-" * 40)
        result = test_func()
        if result is not None:  # None means feature not configured
            results.append(result)
    
    # Summary
    print()
    print("=" * 60)
    print(f"Test Summary: {sum(results)}/{len(results)} tests passed")
    if len(results) < len(tests):
        print(f"Note: {len(tests) - len(results)} test(s) skipped (feature not configured)")
    print("=" * 60)

if __name__ == "__main__":
    main()