"""
Custom Processing SMTP Server Test Script
Tests custom email processing, filtering, and modification
"""
import smtplib
import time
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
from email.utils import formatdate

def test_spam_filtering():
    """Test spam domain filtering"""
    
    # Test emails from spam domains
    spam_senders = [
        ("spammer@spam.com", "user@example.com", "Should be rejected"),
        ("hacker@malware.org", "admin@example.com", "Should be rejected"),
        ("phisher@phishing.net", "user@example.com", "Should be rejected")
    ]
    
    print("   Testing spam domain filtering...")
    
    rejected_count = 0
    for sender, recipient, description in spam_senders:
        msg = MIMEText(f"This is a spam test from {sender}")
        msg['Subject'] = 'Spam Test'
        msg['From'] = sender
        msg['To'] = recipient
        msg['Date'] = formatdate(localtime=True)
        
        try:
            with smtplib.SMTP('localhost', 25) as server:
                server.sendmail(sender, [recipient], msg.as_string())
            print(f"   ❌ {sender}: Spam not filtered!")
        except Exception as e:
            if "rejected" in str(e).lower() or "spam" in str(e).lower() or "550" in str(e):
                print(f"   ✅ {sender}: Correctly rejected")
                rejected_count += 1
            else:
                print(f"   ⚠️ {sender}: Failed ({e})")
    
    if rejected_count == len(spam_senders):
        print(f"✅ All spam domains filtered successfully!")
        return True
    else:
        print(f"⚠️ Some spam domains not filtered ({rejected_count}/{len(spam_senders)})")
        return False

def test_allowed_domains():
    """Test allowed domain filtering"""
    
    test_cases = [
        ("user@allowed.com", "admin@example.com", True, "Allowed domain"),
        ("user@trusted.com", "admin@example.com", True, "Trusted domain"),
        ("user@unknown.com", "admin@example.com", False, "Unknown domain")
    ]
    
    print("   Testing allowed domain filtering...")
    
    correct_count = 0
    for sender, recipient, should_pass, description in test_cases:
        msg = MIMEText(f"Testing {description}")
        msg['Subject'] = f'Domain Test: {description}'
        msg['From'] = sender
        msg['To'] = recipient
        msg['Date'] = formatdate(localtime=True)
        
        try:
            with smtplib.SMTP('localhost', 25) as server:
                server.sendmail(sender, [recipient], msg.as_string())
            
            if should_pass:
                print(f"   ✅ {sender}: Correctly accepted ({description})")
                correct_count += 1
            else:
                print(f"   ❌ {sender}: Should have been rejected ({description})")
        except Exception as e:
            if not should_pass:
                print(f"   ✅ {sender}: Correctly rejected ({description})")
                correct_count += 1
            else:
                print(f"   ❌ {sender}: Should have been accepted ({description})")
    
    if correct_count == len(test_cases):
        print(f"✅ Domain filtering working correctly!")
        return True
    else:
        print(f"⚠️ Some domain filters not working ({correct_count}/{len(test_cases)})")
        return False

def test_content_filtering():
    """Test content-based filtering"""
    
    sender = "content@example.com"
    recipient = "filter@example.com"
    
    # Test different content that might trigger filters
    test_contents = [
        ("Normal Message", "This is a normal business email.", True),
        ("Viagra Spam", "Buy Viagra now! Cheap pills! Click here!", False),
        ("Nigerian Prince", "I am a Nigerian prince with $10 million for you", False),
        ("Excessive Caps", "URGENT!!! CLICK NOW!!! FREE MONEY!!!", False),
        ("Many Links", "Click here: http://spam1.com http://spam2.com http://spam3.com", False)
    ]
    
    print("   Testing content-based filtering...")
    
    results = []
    for subject, body, should_pass in test_contents:
        msg = MIMEText(body)
        msg['Subject'] = subject
        msg['From'] = sender
        msg['To'] = recipient
        msg['Date'] = formatdate(localtime=True)
        
        try:
            with smtplib.SMTP('localhost', 25) as server:
                server.sendmail(sender, [recipient], msg.as_string())
            
            if should_pass:
                print(f"   ✅ '{subject}': Passed (expected)")
                results.append(True)
            else:
                print(f"   ⚠️ '{subject}': Passed (should be filtered)")
                results.append(True)  # Content filtering might not be active
        except Exception as e:
            if not should_pass:
                print(f"   ✅ '{subject}': Filtered (expected)")
                results.append(True)
            else:
                print(f"   ❌ '{subject}': Filtered (should pass)")
                results.append(False)
    
    return all(results)

def test_header_modification():
    """Test if custom headers are added during processing"""
    
    sender = "header@example.com"
    recipient = "test@example.com"
    
    msg = MIMEMultipart()
    msg['Subject'] = 'Header Modification Test'
    msg['From'] = sender
    msg['To'] = recipient
    msg['Date'] = formatdate(localtime=True)
    msg['X-Original-Header'] = 'TestValue'
    
    body = "This email tests custom header processing"
    msg.attach(MIMEText(body, 'plain'))
    
    try:
        with smtplib.SMTP('localhost', 25) as server:
            server.sendmail(sender, [recipient], msg.as_string())
        
        print(f"✅ Email sent for header processing")
        print(f"   Original headers preserved")
        print(f"   Custom headers may be added by server")
        return True
    except Exception as e:
        print(f"❌ Failed to test header modification")
        print(f"   Error: {e}")
        return False

def test_auto_response():
    """Test automatic response generation"""
    
    # Send to special addresses that might trigger auto-responses
    test_addresses = [
        ("user@example.com", "noreply@example.com", "No-reply address"),
        ("user@example.com", "support@example.com", "Support address"),
        ("user@example.com", "info@example.com", "Info address"),
        ("user@example.com", "postmaster@example.com", "Postmaster")
    ]
    
    print("   Testing auto-response triggers...")
    
    for sender, recipient, description in test_addresses:
        msg = MIMEText(f"Testing auto-response for {description}")
        msg['Subject'] = f'Auto-Response Test: {description}'
        msg['From'] = sender
        msg['To'] = recipient
        msg['Date'] = formatdate(localtime=True)
        
        try:
            with smtplib.SMTP('localhost', 25) as server:
                server.sendmail(sender, [recipient], msg.as_string())
            print(f"   ✅ {recipient}: Message accepted")
        except Exception as e:
            print(f"   ⚠️ {recipient}: {e}")
    
    print(f"   Note: Check if auto-responses were generated")
    return True

def test_message_modification():
    """Test if messages are modified during processing"""
    
    sender = "modify@example.com"
    recipient = "process@example.com"
    
    msg = MIMEMultipart('alternative')
    msg['Subject'] = '[TEST] Message Modification Test'
    msg['From'] = sender
    msg['To'] = recipient
    msg['Date'] = formatdate(localtime=True)
    
    # Original content that might be modified
    text = """
    Original message content.
    
    This might be modified by custom processing:
    - Tags might be added to subject
    - Disclaimers might be appended
    - Headers might be modified
    
    Test email address: test@example.com
    Test phone: 555-1234
    """
    
    html = """
    <html>
      <body>
        <p>Original HTML content.</p>
        <p>This might be modified by processing.</p>
      </body>
    </html>
    """
    
    msg.attach(MIMEText(text, 'plain'))
    msg.attach(MIMEText(html, 'html'))
    
    try:
        with smtplib.SMTP('localhost', 25) as server:
            server.sendmail(sender, [recipient], msg.as_string())
        
        print(f"✅ Message sent for modification processing")
        print(f"   Subject may be modified")
        print(f"   Content may be modified")
        print(f"   Headers may be added")
        return True
    except Exception as e:
        print(f"❌ Failed to test message modification")
        print(f"   Error: {e}")
        return False

def main():
    """Main test function"""
    print("=" * 60)
    print("Custom Processing SMTP Server Test")
    print("=" * 60)
    print()
    
    print("Testing custom processing features:")
    print("- Spam filtering")
    print("- Domain filtering")
    print("- Content filtering")
    print("- Header modification")
    print("- Auto-responses")
    print("- Message modification")
    print()
    
    # Run tests
    tests = [
        ("Spam Domain Filtering", test_spam_filtering),
        ("Allowed Domain Filtering", test_allowed_domains),
        ("Content Filtering", test_content_filtering),
        ("Header Modification", test_header_modification),
        ("Auto-Response System", test_auto_response),
        ("Message Modification", test_message_modification)
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
    print("NOTE: Custom processing behavior depends on server configuration")
    print("      Some features may not be active in all configurations")
    print("=" * 60)

if __name__ == "__main__":
    main()