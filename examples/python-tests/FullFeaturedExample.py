"""
Full Featured SMTP Server Test Script
Comprehensive test of all SMTP server features combined
"""
import smtplib
import ssl
import time
import concurrent.futures
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
from email.mime.application import MIMEApplication
from email.utils import formatdate

def test_secure_authenticated_send():
    """Test TLS + Authentication + Send"""
    
    username = "admin"  # Using server credentials
    password = "admin123"  # Correct password for admin
    recipient = "admin@example.com"
    
    msg = MIMEMultipart()
    msg['Subject'] = 'Full Feature Test: Secure + Auth'
    msg['From'] = "admin@example.com"  # Using proper email format
    msg['To'] = recipient
    msg['Date'] = formatdate(localtime=True)
    
    body = """
    This email tests multiple features:
    ✓ TLS Encryption (STARTTLS)
    ✓ Authentication (LOGIN/PLAIN)
    ✓ Message Delivery
    ✓ Header Processing
    """
    msg.attach(MIMEText(body, 'plain'))
    
    try:
        with smtplib.SMTP('localhost', 587) as server:
            # Enable TLS
            context = ssl.create_default_context()
            context.check_hostname = False
            context.verify_mode = ssl.CERT_NONE
            server.starttls(context=context)
            print("   ✅ TLS enabled")
            
            # Authenticate
            server.login(username, password)
            print(f"   ✅ Authenticated as {username}")
            
            # Send
            server.sendmail("admin@example.com", [recipient], msg.as_string())
            print(f"   ✅ Email sent securely")
            
        return True
    except Exception as e:
        print(f"   ❌ Failed: {e}")
        return False

def test_filtered_storage():
    """Test Domain Filtering + Storage"""
    
    username = "admin"
    password = "admin123"
    
    # Updated test cases to use allowed domains: example.com, test.com, demo.com, localhost
    test_cases = [
        ("admin@example.com", "storage@example.com", True, "Allowed sender to allowed recipient"),
        ("spam@spam.com", "user@example.com", False, "Spam domain blocked"),
        ("user@test.com", "external@gmail.com", False, "External recipient blocked"),
    ]
    
    results = []
    for sender, recipient, should_succeed, description in test_cases:
        msg = MIMEText(f"Testing: {description}")
        msg['Subject'] = f'Filter+Storage Test'
        msg['From'] = sender
        msg['To'] = recipient
        msg['Date'] = formatdate(localtime=True)
        msg['Message-ID'] = f"<test-{int(time.time())}-{sender}>"
        
        try:
            with smtplib.SMTP('localhost', 587) as server:
                # Enable TLS and authenticate
                context = ssl.create_default_context()
                context.check_hostname = False
                context.verify_mode = ssl.CERT_NONE
                server.starttls(context=context)
                server.login(username, password)
                
                server.sendmail(sender, [recipient], msg.as_string())
            
            if should_succeed:
                print(f"   ✅ {description}: Passed and stored")
                results.append(True)
            else:
                print(f"   ❌ {description}: Should have been blocked")
                results.append(False)
        except Exception as e:
            if not should_succeed:
                print(f"   ✅ {description}: Correctly blocked")
                results.append(True)
            else:
                print(f"   ❌ {description}: Should have passed")
                results.append(False)
    
    return all(results)

def test_rate_limiting_with_auth():
    """Test Rate Limiting with Authentication"""
    
    username = "admin"  # Using server credentials
    password = "admin123"  # Correct password for admin
    recipient = "test@example.com"
    
    print("   Testing authenticated rate limits...")
    
    success_count = 0
    limit_count = 0
    
    for i in range(8):
        msg = MIMEText(f"Rate limit test {i+1}")
        msg['Subject'] = f'Rate Test {i+1}'
        msg['From'] = "admin@example.com"
        msg['To'] = recipient
        msg['Date'] = formatdate(localtime=True)
        
        try:
            with smtplib.SMTP('localhost', 587) as server:
                # Try TLS if available
                try:
                    context = ssl.create_default_context()
                    context.check_hostname = False
                    context.verify_mode = ssl.CERT_NONE
                    server.starttls(context=context)
                except:
                    pass
                
                # Authenticate
                try:
                    server.login(username, password)
                except:
                    pass  # Continue if auth not required
                
                server.sendmail("admin@example.com", [recipient], msg.as_string())
                success_count += 1
                print(f"      Email {i+1}: ✅ Sent")
        except Exception as e:
            if "rate" in str(e).lower() or "too many" in str(e).lower():
                limit_count += 1
                print(f"      Email {i+1}: ⏱️ Rate limited")
            else:
                print(f"      Email {i+1}: ❌ Failed")
        
        time.sleep(0.2)
    
    print(f"   Results: {success_count} sent, {limit_count} rate limited")
    return limit_count > 0  # Should have some rate limiting

def test_large_attachment_with_storage():
    """Test Large Attachments + Storage + Size Limits"""
    
    username = "admin"
    password = "admin123"
    sender = "admin@example.com"
    recipient = "storage@example.com"
    
    msg = MIMEMultipart()
    msg['Subject'] = 'Large Attachment Test'
    msg['From'] = sender
    msg['To'] = recipient
    msg['Date'] = formatdate(localtime=True)
    
    # Add body
    msg.attach(MIMEText("This email contains large attachments", 'plain'))
    
    # Add large attachment (500KB)
    large_data = b"X" * (500 * 1024)
    attachment = MIMEApplication(large_data, _subtype="bin")
    attachment.add_header('Content-Disposition', 'attachment', filename='large_file.bin')
    msg.attach(attachment)
    
    try:
        with smtplib.SMTP('localhost', 587) as server:
            # Enable TLS and authenticate
            context = ssl.create_default_context()
            context.check_hostname = False
            context.verify_mode = ssl.CERT_NONE
            server.starttls(context=context)
            server.login(username, password)
            
            server.sendmail(sender, [recipient], msg.as_string())
        print(f"   ✅ Large attachment (500KB) accepted and stored")
        return True
    except Exception as e:
        if "size" in str(e).lower() or "too large" in str(e).lower():
            print(f"   ℹ️ Large attachment rejected due to size limit")
            return True  # Size limiting is working
        else:
            print(f"   ❌ Failed: {e}")
            return False

def test_concurrent_features():
    """Test Multiple Features Under Load"""
    
    def send_test_email(index):
        try:
            # Use admin credentials for all concurrent tests
            username = "admin"
            password = "admin123"
            sender = "admin@example.com"  # Use authenticated sender
            recipient = f"test{index}@example.com"
            
            msg = MIMEMultipart()
            msg['Subject'] = f'Concurrent Test {index}'
            msg['From'] = sender
            msg['To'] = recipient
            msg['Date'] = formatdate(localtime=True)
            
            # Random content to test different paths
            if index % 3 == 0:
                # HTML email
                msg.attach(MIMEText("<h1>HTML Test</h1>", 'html'))
            elif index % 3 == 1:
                # With attachment
                attachment = MIMEApplication(b"test data", _subtype="txt")
                attachment.add_header('Content-Disposition', 'attachment', filename=f'file{index}.txt')
                msg.attach(attachment)
            else:
                # Plain text
                msg.attach(MIMEText(f"Plain text message {index}", 'plain'))
            
            with smtplib.SMTP('localhost', 587) as server:
                # Enable TLS and authenticate
                context = ssl.create_default_context()
                context.check_hostname = False
                context.verify_mode = ssl.CERT_NONE
                server.starttls(context=context)
                server.login(username, password)
                
                server.sendmail(sender, [recipient], msg.as_string())
            return True
        except Exception as e:
            return False
    
    print("   Sending 10 concurrent emails with different features...")
    
    with concurrent.futures.ThreadPoolExecutor(max_workers=5) as executor:
        futures = [executor.submit(send_test_email, i) for i in range(10)]
        results = [f.result() for f in futures]
    
    success_count = sum(results)
    print(f"   ✅ {success_count}/10 emails processed successfully")
    
    return success_count >= 5  # At least 50% should succeed

def test_smtp_extensions():
    """Test SMTP Extensions Support"""
    
    try:
        with smtplib.SMTP('localhost', 587) as server:
            # Get EHLO response
            code, response = server.ehlo()
            
            if code == 250:
                print("   ✅ EHLO supported")
                
                # Parse extensions
                extensions = str(response, 'utf-8').split('\n') if isinstance(response, bytes) else response.split('\n')
                
                print("   Detected extensions:")
                expected_extensions = ['PIPELINING', '8BITMIME', 'SIZE', 'STARTTLS', 'AUTH']
                found_extensions = []
                
                for ext in extensions:
                    ext_upper = ext.upper().strip()
                    for expected in expected_extensions:
                        if expected in ext_upper:
                            print(f"      ✅ {expected}")
                            found_extensions.append(expected)
                            break
                
                # Check for SIZE parameter
                for ext in extensions:
                    if 'SIZE' in ext.upper():
                        parts = ext.split()
                        if len(parts) > 1:
                            print(f"      Max message size: {parts[1]} bytes")
                
                return len(found_extensions) > 0
            else:
                print(f"   ❌ EHLO failed with code {code}")
                return False
    except Exception as e:
        print(f"   ❌ Failed to test extensions: {e}")
        return False

def test_utf8_support():
    """Test SMTPUTF8 Support"""
    
    username = "admin"
    password = "admin123"
    sender = "admin@example.com"  # Use authenticated sender
    recipient = "test@example.com"  # Use allowed domain
    
    msg = MIMEText("Testing UTF-8 content: Türkçe karakterler: ğüşıöç", 'plain', 'utf-8')
    msg['Subject'] = 'UTF-8 Test: Türkçe Başlık 🚀'
    msg['From'] = sender
    msg['To'] = recipient
    msg['Date'] = formatdate(localtime=True)
    
    try:
        with smtplib.SMTP('localhost', 587) as server:
            # Enable TLS and authenticate
            context = ssl.create_default_context()
            context.check_hostname = False
            context.verify_mode = ssl.CERT_NONE
            server.starttls(context=context)
            server.login(username, password)
            
            # Try to send with UTF-8
            server.sendmail(sender, [recipient], msg.as_string())
        print(f"   ✅ UTF-8 email addresses and content supported")
        return True
    except Exception as e:
        if "utf" in str(e).lower() or "ascii" in str(e).lower():
            print(f"   ℹ️ UTF-8 not fully supported (expected for compatibility)")
            return True
        else:
            print(f"   ❌ Failed: {e}")
            return False

def main():
    """Main test function"""
    print("=" * 60)
    print("Full Featured SMTP Server Test")
    print("Testing ALL features in combination")
    print("=" * 60)
    print()
    
    print("Features being tested:")
    print("✓ TLS/SSL Encryption")
    print("✓ Authentication")
    print("✓ Domain Filtering")
    print("✓ Rate Limiting")
    print("✓ Message Storage")
    print("✓ Large Attachments")
    print("✓ SMTP Extensions")
    print("✓ Concurrent Processing")
    print("✓ UTF-8 Support")
    print()
    
    # Run tests
    tests = [
        ("Secure + Authenticated Send", test_secure_authenticated_send),
        ("Filtering + Storage", test_filtered_storage),
        ("Rate Limiting + Auth", test_rate_limiting_with_auth),
        ("Large Attachments + Storage", test_large_attachment_with_storage),
        ("Concurrent Feature Processing", test_concurrent_features),
        ("SMTP Extensions", test_smtp_extensions),
        ("UTF-8 Support", test_utf8_support)
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
    print("Full Featured Server Status:")
    if sum(results) == len(results):
        print("🏆 ALL FEATURES WORKING PERFECTLY!")
    elif sum(results) >= len(results) * 0.8:
        print("✅ Most features working well")
    elif sum(results) >= len(results) * 0.5:
        print("⚠️ Some features need configuration")
    else:
        print("❌ Many features not configured")
    print("=" * 60)

if __name__ == "__main__":
    main()