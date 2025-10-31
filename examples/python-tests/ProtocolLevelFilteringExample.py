import smtplib
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
from email.utils import formatdate

def test_email(sender, recipient, subject, description, should_succeed=True):
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
        
        if should_succeed:
            print(f"✅ PASSED: {description}")
            print(f"   Email sent successfully: {sender} -> {recipient}")
        else:
            print(f"❌ FAILED: {description}")
            print(f"   Email should have been rejected but was sent!")
    except Exception as e:
        if not should_succeed:
            print(f"✅ PASSED: {description}")
            print(f"   Email correctly rejected: {e}")
        else:
            print(f"❌ FAILED: {description}")
            print(f"   Email should have been sent but got error: {e}")
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
        # Format: (sender, recipient, subject, description, should_succeed)
        
        # Successful scenarios
        ("sender@trusted.com", "user@mydomain.com", "Valid sender and recipient", 
         "Both domains are whitelisted", True),
        
        ("admin@example.com", "support@example.com", "Both from example.com", 
         "example.com is allowed for both", True),
        
        # Sender rejection scenarios - SHOULD BE BLOCKED
        ("spammer@spam.com", "user@mydomain.com", "Blocked sender domain", 
         "spam.com is blacklisted - should be rejected", False),
        
        ("hacker@junk.org", "admin@example.com", "Another blocked sender", 
         "junk.org is blacklisted - should be rejected", False),
        
        # Recipient rejection scenarios - SHOULD BE BLOCKED
        ("user@trusted.com", "someone@external.com", "External recipient", 
         "external.com not in recipient whitelist - should be rejected", False),
        
        ("admin@example.com", "user@gmail.com", "Gmail recipient", 
         "gmail.com not in recipient whitelist - should be rejected", False),
        
        # Mixed mode test (both whitelist and blacklist active)
        ("user@other.com", "admin@mydomain.com", "Unlisted sender domain", 
         "other.com not blocked, mydomain.com allowed", True),
        
        # Event-based filtering test - SHOULD BE BLOCKED BY EVENT FILTER
        ("attacker@phishing.net", "user@example.com", "Phishing domain", 
         "Should be caught by event-based filter", False),
        
        ("bot@malware.org", "admin@localhost", "Malware domain", 
         "Should be caught by event-based filter", False),
    ]
    
    for sender, recipient, subject, description, should_succeed in scenarios:
        test_email(sender, recipient, subject, description, should_succeed)
    
    print()
    print("=" * 60)
    print("Test Summary:")
    print("- Protocol-level filters reject at SMTP command level")
    print("- Event-based filters reject after message received")
    print("- Mixed mode: whitelist + blacklist + default policy")
    print("=" * 60)

if __name__ == "__main__":
    main()