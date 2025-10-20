"""
Basic SMTP Server Test Script
Tests basic email sending functionality on port 25
"""
import smtplib
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
from email.utils import formatdate

def test_basic_smtp():
    """Test basic SMTP functionality"""
    
    # Email configuration
    sender = "test@example.com"
    recipients = ["user@example.com", "admin@example.com"]
    
    # Create message
    msg = MIMEMultipart()
    msg['Subject'] = 'Basic SMTP Test'
    msg['From'] = sender
    msg['To'] = ', '.join(recipients)
    msg['Date'] = formatdate(localtime=True)
    
    # Email body
    body = """
Hello,

This is a basic SMTP server test.
Testing simple email delivery without authentication.

Features tested:
- Basic SMTP connection
- Simple email delivery
- Multiple recipients
- Text email format

Best regards,
Test System
"""
    msg.attach(MIMEText(body, 'plain'))
    
    try:
        # Connect and send
        with smtplib.SMTP('localhost', 25) as server:
            server.sendmail(sender, recipients, msg.as_string())
        print(f"✅ Basic email sent successfully!")
        print(f"   From: {sender}")
        print(f"   To: {', '.join(recipients)}")
        return True
    except Exception as e:
        print(f"❌ Failed to send basic email")
        print(f"   Error: {e}")
        return False

def test_large_message():
    """Test sending a large message"""
    sender = "test@example.com"
    recipient = "user@example.com"
    
    msg = MIMEMultipart()
    msg['Subject'] = 'Large Message Test'
    msg['From'] = sender
    msg['To'] = recipient
    msg['Date'] = formatdate(localtime=True)
    
    # Create a large message body (1MB)
    large_body = "This is a test of large message handling.\n" * 50000
    msg.attach(MIMEText(large_body, 'plain'))
    
    try:
        with smtplib.SMTP('localhost', 25) as server:
            server.sendmail(sender, [recipient], msg.as_string())
        print(f"✅ Large message sent successfully! (Size: ~{len(large_body)/1024:.0f}KB)")
        return True
    except Exception as e:
        print(f"❌ Failed to send large message")
        print(f"   Error: {e}")
        return False

def test_multiple_connections():
    """Test multiple simultaneous connections"""
    import concurrent.futures
    
    def send_email(index):
        sender = f"sender{index}@example.com"
        recipient = f"user{index}@example.com"
        
        msg = MIMEText(f"Test message {index}")
        msg['Subject'] = f'Connection Test {index}'
        msg['From'] = sender
        msg['To'] = recipient
        msg['Date'] = formatdate(localtime=True)
        
        try:
            with smtplib.SMTP('localhost', 25) as server:
                server.sendmail(sender, [recipient], msg.as_string())
            return True
        except:
            return False
    
    # Send 5 emails concurrently
    with concurrent.futures.ThreadPoolExecutor(max_workers=5) as executor:
        futures = [executor.submit(send_email, i) for i in range(5)]
        results = [f.result() for f in futures]
    
    success_count = sum(results)
    print(f"✅ Multiple connections: {success_count}/5 successful")
    return all(results)

if __name__ == "__main__":
    print("=" * 60)
    print("Basic SMTP Server Test")
    print("=" * 60)
    print()
    
    # Run tests
    tests = [
        ("Basic Email", test_basic_smtp),
        ("Large Message", test_large_message),
        ("Multiple Connections", test_multiple_connections)
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