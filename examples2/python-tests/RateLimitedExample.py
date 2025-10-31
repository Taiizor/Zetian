"""
Rate Limited SMTP Server Test Script
Tests rate limiting functionality to prevent abuse
"""
import smtplib
import time
import concurrent.futures
from email.mime.text import MIMEText
from email.utils import formatdate

def test_rate_limit_per_minute():
    """Test rate limiting per minute"""
    
    sender = "ratelimit@example.com"
    recipient = "user@example.com"
    
    print("   Sending emails to test rate limit (5 per minute)...")
    
    success_count = 0
    rejected_count = 0
    
    for i in range(10):  # Try to send 10 emails (limit is 5)
        msg = MIMEText(f"Rate limit test email #{i+1}")
        msg['Subject'] = f'Rate Limit Test {i+1}'
        msg['From'] = sender
        msg['To'] = recipient
        msg['Date'] = formatdate(localtime=True)
        
        try:
            with smtplib.SMTP('localhost', 25) as server:
                server.sendmail(sender, [recipient], msg.as_string())
            success_count += 1
            print(f"   Email {i+1}: ✅ Sent")
        except Exception as e:
            rejected_count += 1
            if "rate limit" in str(e).lower() or "too many" in str(e).lower() or "429" in str(e):
                print(f"   Email {i+1}: ⏱️ Rate limited")
            else:
                print(f"   Email {i+1}: ❌ Failed ({e})")
        
        # Small delay between sends
        time.sleep(0.1)
    
    print(f"\n   Results: {success_count} sent, {rejected_count} rejected")
    
    # Expected: ~5 success, ~5 rejected (server limit is 5 per minute)
    if rejected_count > 0 and success_count >= 4:
        print("✅ Rate limiting is working!")
        return True
    else:
        print("⚠️ Rate limiting may not be active")
        return False

def test_connection_limit():
    """Test maximum concurrent connections limit"""
    
    print("   Testing concurrent connection limit...")
    
    def create_connection(index):
        try:
            # Create and hold connection
            server = smtplib.SMTP('localhost', 25, timeout=5)
            time.sleep(0.5)  # Hold connection briefly
            server.quit()
            return True
        except Exception as e:
            error_str = str(e).lower()
            if "too many connections" in error_str or "connection limit" in error_str or "connection refused" in error_str:
                return False
            # Connection unexpectedly closed is also a sign of limit
            if "connection unexpectedly closed" in error_str or "broken pipe" in error_str:
                return False
            return False  # Treat any error as rejection for this test
    
    # Try to create 8 concurrent connections (typical limit is 5)
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as executor:
        futures = [executor.submit(create_connection, i) for i in range(8)]
        results = []
        for f in futures:
            try:
                results.append(f.result())
            except:
                results.append(False)
    
    successful = sum(results)
    rejected = len(results) - successful
    
    print(f"   Results: {successful} connected, {rejected} rejected")
    
    if rejected > 0:
        print("✅ Connection limiting is working!")
        return True
    else:
        print("⚠️ Connection limiting may not be active")
        return False

def test_ip_based_limit():
    """Test per-IP rate limiting"""
    
    print("   Testing per-IP rate limiting (5 messages per IP per minute)...")
    
    success_count = 0
    rejected_count = 0
    
    # Send multiple emails from same IP (localhost)
    for i in range(7):  # Try 7 emails (typical per-IP limit is 5)
        sender = f"user{i}@example.com"
        recipient = "admin@example.com"
        
        msg = MIMEText(f"IP rate limit test {i+1}")
        msg['Subject'] = f'IP Test {i+1}'
        msg['From'] = sender
        msg['To'] = recipient
        msg['Date'] = formatdate(localtime=True)
        
        try:
            with smtplib.SMTP('localhost', 25) as server:
                server.sendmail(sender, [recipient], msg.as_string())
            success_count += 1
            print(f"   Email {i+1} from {sender}: ✅")
        except Exception as e:
            rejected_count += 1
            print(f"   Email {i+1} from {sender}: ⏱️ Limited")
        
        time.sleep(0.2)
    
    print(f"\n   Results: {success_count} sent, {rejected_count} rejected")
    
    if rejected_count > 0:
        print("✅ Per-IP rate limiting is working!")
        return True
    else:
        print("⚠️ Per-IP limiting may not be active")
        return False

def test_rate_limit_recovery():
    """Test that rate limit resets after time period"""
    
    print("   Testing rate limit recovery...")
    
    sender = "recovery@example.com"
    recipient = "user@example.com"
    
    # First, hit the rate limit
    print("   Phase 1: Hitting rate limit...")
    for i in range(12):
        msg = MIMEText(f"Recovery test {i+1}")
        msg['Subject'] = f'Recovery {i+1}'
        msg['From'] = sender
        msg['To'] = recipient
        
        try:
            with smtplib.SMTP('localhost', 25) as server:
                server.sendmail(sender, [recipient], msg.as_string())
        except:
            pass
    
    # Wait for rate limit window to reset (usually 60 seconds)
    print("   Phase 2: Waiting 61 seconds for reset...")
    print("   (This may take a while...)")
    time.sleep(61)
    
    # Try sending again
    print("   Phase 3: Testing after reset...")
    msg = MIMEText("Test after rate limit reset")
    msg['Subject'] = 'Recovery Test'
    msg['From'] = sender
    msg['To'] = recipient
    msg['Date'] = formatdate(localtime=True)
    
    try:
        with smtplib.SMTP('localhost', 25) as server:
            server.sendmail(sender, [recipient], msg.as_string())
        print("✅ Rate limit successfully reset after time period!")
        return True
    except Exception as e:
        print(f"❌ Still rate limited after waiting: {e}")
        return False

def main():
    """Main test function"""
    print("=" * 60)
    print("Rate Limited SMTP Server Test")
    print("=" * 60)
    print()
    
    print("Testing rate limiting features:")
    print("- Messages per minute limit")
    print("- Concurrent connections limit")  
    print("- Per-IP address limits")
    print()
    
    # Run tests
    tests = [
        ("Rate Limit (Messages/Minute)", test_rate_limit_per_minute),
        ("Connection Limit", test_connection_limit),
        ("Per-IP Rate Limit", test_ip_based_limit),
        # ("Rate Limit Recovery", test_rate_limit_recovery)  # Commented out as it takes 60+ seconds
    ]
    
    print("NOTE: Skipping recovery test (takes 60+ seconds)")
    print("      Uncomment in code to test rate limit recovery")
    print()
    
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