#!/usr/bin/env python3
"""
Test script for Zetian SMTP Relay functionality
Tests if messages are properly queued for relay
"""

import smtplib
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
from datetime import datetime
import time

def send_test_email(smtp_host="localhost", smtp_port=25025, 
                    from_addr="test@python.local", 
                    to_addr="recipient@external.com",
                    subject_prefix="Python Relay Test"):
    """Send a test email through the SMTP server"""
    
    # Create message
    msg = MIMEMultipart()
    msg['From'] = from_addr
    msg['To'] = to_addr
    msg['Subject'] = f"{subject_prefix} - {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}"
    
    # Body
    body = f"""
This is a test email sent from Python to test relay functionality.

Test Details:
- Sent from: {from_addr}
- Sent to: {to_addr}
- Time: {datetime.now()}
- SMTP Server: {smtp_host}:{smtp_port}

This message should be queued for relay if the recipient domain is external.
"""
    
    msg.attach(MIMEText(body, 'plain'))
    
    try:
        # Connect to server
        print(f"Connecting to {smtp_host}:{smtp_port}...")
        server = smtplib.SMTP(smtp_host, smtp_port)
        server.set_debuglevel(1)  # Enable debug output
        
        # Send email
        print(f"Sending email from {from_addr} to {to_addr}...")
        text = msg.as_string()
        result = server.sendmail(from_addr, to_addr, text)
        
        # Close connection
        server.quit()
        
        print(f"✓ Email sent successfully!")
        if result:
            print(f"  Server response: {result}")
        return True
        
    except Exception as e:
        print(f"✗ Failed to send email: {e}")
        return False

def test_relay_scenarios():
    """Test different relay scenarios"""
    
    print("="*50)
    print("ZETIAN SMTP RELAY TEST")
    print("="*50)
    print()
    
    test_cases = [
        # (from, to, description)
        ("sender@python.local", "external@gmail.com", "External domain - should relay"),
        ("sender@python.local", "local@localhost", "Localhost - should NOT relay"),
        ("sender@python.local", "local@relay.local", "Local domain - should NOT relay"),
        ("sender@python.local", "mixed@yahoo.com", "Another external - should relay"),
        ("sender@relay.local", "test@outlook.com", "From local to external - should relay"),
    ]
    
    for idx, (from_addr, to_addr, description) in enumerate(test_cases, 1):
        print(f"\nTest #{idx}: {description}")
        print(f"  From: {from_addr}")
        print(f"  To: {to_addr}")
        print()
        
        success = send_test_email(
            from_addr=from_addr,
            to_addr=to_addr,
            subject_prefix=f"Test #{idx}"
        )
        
        if success:
            print(f"  Result: Message accepted by server")
        else:
            print(f"  Result: Failed to send")
        
        print("-"*40)
        time.sleep(1)  # Small delay between tests
    
    print("\n" + "="*50)
    print("TESTS COMPLETED")
    print("Check the relay queue statistics to verify messages were queued correctly.")
    print("External recipients should show in queue, local recipients should not.")
    print("="*50)

if __name__ == "__main__":
    # You can customize these settings
    SMTP_HOST = "localhost"
    SMTP_PORT = 25025  # BasicRelayExample port
    
    # Run single test
    print("Sending single test email...")
    send_test_email(smtp_host=SMTP_HOST, smtp_port=SMTP_PORT)
    
    print("\n" + "="*50)
    print("\nRun full test suite? (y/n): ", end="")
    if input().lower() == 'y':
        test_relay_scenarios()