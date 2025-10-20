import smtplib
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
from email.utils import formatdate

def test_email(sender, recipient, subject, description):
    """Test email sending with specific scenario"""
    msg = MIMEMultipart()
    msg['Subject'] = subject
    msg['From'] = sender
    msg['To'] = recipient
    msg['Date'] = formatdate(localtime=True)
    
    body = f"""
Test Scenario: {description}
From: {sender}
To: {recipient}

This email tests the filtering rules of the SMTP server.
"""
    msg.attach(MIMEText(body, 'plain'))
    
    try:
        with smtplib.SMTP('localhost', 25) as server:
            # Use the actual sender address for MAIL FROM
            server.sendmail(sender, [recipient], msg.as_string())
        print(f"✅ SUCCESS: {description}")
        print(f"   From: {sender} -> To: {recipient}")
    except Exception as e:
        print(f"❌ FAILED: {description}")
        print(f"   From: {sender} -> To: {recipient}")
        print(f"   Error: {e}")
    print("-" * 50)

def main():
    """Main test function"""
    print("=" * 60)
    print("SMTP Server Protocol-Level Filtering Test")
    print("=" * 60)
    print()
    
    # Test scenarios based on server configuration:
    # Allowed sender domains: trusted.com, example.com, localhost
    # Blocked sender domains: spam.com, junk.org
    # Allowed recipient domains: mydomain.com, example.com, localhost
    
    scenarios = [
        # Successful scenarios
        ("sender@trusted.com", "user@mydomain.com", "Valid sender and recipient", 
         "Should succeed - both domains are whitelisted"),
        
        ("admin@example.com", "support@example.com", "Both from example.com", 
         "Should succeed - example.com is allowed for both"),
        
        # Sender rejection scenarios
        ("spammer@spam.com", "user@mydomain.com", "Blocked sender domain", 
         "Should fail - spam.com is blacklisted"),
        
        ("hacker@junk.org", "admin@example.com", "Another blocked sender", 
         "Should fail - junk.org is blacklisted"),
        
        # Recipient rejection scenarios  
        ("user@trusted.com", "someone@external.com", "External recipient", 
         "Should fail - external.com not in recipient whitelist"),
        
        ("admin@example.com", "user@gmail.com", "Gmail recipient", 
         "Should fail - gmail.com not in recipient whitelist"),
        
        # Mixed mode test (both whitelist and blacklist active)
        ("user@other.com", "admin@mydomain.com", "Unlisted sender domain", 
         "Should succeed - other.com not blocked, mydomain.com allowed"),
        
        # Event-based filtering test (these pass protocol checks)
        ("attacker@phishing.net", "user@example.com", "Phishing domain", 
         "Protocol pass, event-based filter should catch"),
        
        ("bot@malware.org", "admin@localhost", "Malware domain", 
         "Protocol pass, event-based filter should catch"),
    ]
    
    for sender, recipient, subject, description in scenarios:
        test_email(sender, recipient, subject, description)
    
    print()
    print("=" * 60)
    print("Test Summary:")
    print("- Protocol-level filters reject at SMTP command level")
    print("- Event-based filters reject after message received")
    print("- Mixed mode: whitelist + blacklist + default policy")
    print("=" * 60)

if __name__ == "__main__":
    main()