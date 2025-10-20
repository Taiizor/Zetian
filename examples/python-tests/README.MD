# Zetian SMTP Server - Python Test Scripts

This folder contains Python test scripts designed to test all features of the Zetian SMTP Server.

## 📋 Available Test Scripts

| Test File | Description | Features Tested |
|-----------|-------------|-----------------|
| `BasicExample.py` | Basic SMTP functionality | Simple email sending, multiple recipients, large messages |
| `AuthenticatedExample.py` | SMTP authentication | PLAIN/LOGIN auth, TLS, wrong credentials |
| `SecureExample.py` | TLS/SSL encryption | STARTTLS, Implicit TLS, cipher information |
| `RateLimitedExample.py` | Rate limiting | Per-minute limit, IP limit, connection limit |
| `MessageStorageExample.py` | Message storage | File system, attachments, HTML emails |
| `CustomProcessingExample.py` | Custom processing | Spam filtering, domain filtering, content control |
| `FullFeaturedExample.py` | All features | TLS + Auth + Storage + Filtering combination |
| `ProtocolLevelFilteringExample.py` | Protocol-level filtering | Early vs Late rejection comparison |

## 🚀 Quick Start

### 1. Start the Zetian SMTP Server

First, run the C# example project:
```bash
cd examples/Zetian.Examples
dotnet run
```

Select the example you want to test from the menu (1-8).

### 2. Run the Python Test Script

Run the corresponding Python script in the same folder:

```bash
# Run a single test
python BasicExample.py

# Or use the main test runner
python run_all_tests.py
```

## 🎯 Main Test Runner Usage

`run_all_tests.py` is used to manage all tests:

### Interactive Mode
```bash
python run_all_tests.py
```
Select the test you want from the menu.

### Command Line Mode
```bash
# Run all tests
python run_all_tests.py all

# Run a specific test (1-8)
python run_all_tests.py 1  # Runs BasicExample.py
```

## 📊 Test Scenarios

### BasicExample.py
- ✅ Simple email sending
- ✅ Multiple recipient support
- ✅ Large message sending (1MB+)
- ✅ Multiple connection test

### AuthenticatedExample.py
- ✅ PLAIN authentication
- ✅ LOGIN authentication
- ✅ Wrong credentials rejection
- ✅ Sending without authentication rejection

### SecureExample.py
- ✅ STARTTLS (port 587)
- ✅ Implicit TLS (port 465)
- ✅ Plaintext auth rejection (TLS required)
- ✅ Cipher information display

### RateLimitedExample.py
- ✅ Per-minute message limit (5/minute)
- ✅ Maximum connection limit (5 concurrent)
- ✅ Per-IP limit (5/IP)
- ⏱️ Rate limit reset (60 seconds)

### MessageStorageExample.py
- ✅ File system storage (.eml format)
- ✅ Emails with attachments
- ✅ HTML format preservation
- ✅ Date-based folder organization
- ✅ Large email support

### CustomProcessingExample.py
- ✅ Spam domain filtering
- ✅ Allowed domain control
- ✅ Content-based filtering
- ✅ Header modification
- ✅ Auto-response system

### FullFeaturedExample.py
- ✅ TLS + Authentication
- ✅ Filtering + Storage
- ✅ Rate Limiting + Auth
- ✅ Large attachments
- ✅ SMTP extensions
- ✅ UTF-8 support

### ProtocolLevelFilteringExample.py
- ✅ Protocol-level rejection (MAIL FROM/RCPT TO)
- ✅ Event-based rejection (after DATA)
- ✅ Mixed mode filtering
- ✅ Bandwidth saving comparison

## 🔧 Test Configuration

### Default Ports
- **25**: Simple SMTP (no auth)
- **587**: Submission port (STARTTLS + Auth)
- **465**: Implicit TLS (SSL)

### Default Credentials
```python
# For AuthenticatedExample.py (matching server configuration)
username = "admin"
password = "password123"
```

### Domain Filtering Settings
```python
# Allowed sender domains
allowed_senders = ["trusted.com", "example.com", "localhost"]

# Blocked sender domains
blocked_senders = ["spam.com", "junk.org"]

# Allowed recipient domains
allowed_recipients = ["mydomain.com", "example.com", "localhost"]

# Event-based spam domains
spam_domains = ["phishing.net", "malware.org"]
```

## 📈 Interpreting Test Results

### Successful Test
```
✅ SUCCESS: Test description
   From: sender@example.com -> To: recipient@example.com
```

### Failed Test (Expected)
```
❌ FAILED: Test description (expected behavior)
   Error: 550 Sender rejected
```
This is **normal** behavior in filtering tests.

### Real Error
```
❌ FAILED: Test description
   Error: Connection refused
```
In this case, the SMTP server may not be running.

## 🐛 Troubleshooting

### "Connection refused" error
- Make sure the SMTP server is running
- Check that it's listening on the correct port
- Check firewall settings

### "Authentication failed" error
- Check credentials
- Make sure authentication is enabled on the server
- TLS may be required (port 587)

### "Rate limit exceeded" error
- This is normal behavior
- Wait 60 seconds or restart the server

### "Message rejected by filter" error
- Check domain filtering settings
- Review spam lists

## 📚 Additional Resources

- [Zetian SMTP Server Documentation](../../README.md)
- [SMTP Protocol RFC 5321](https://tools.ietf.org/html/rfc5321)
- [SMTP Auth RFC 4954](https://tools.ietf.org/html/rfc4954)
- [STARTTLS RFC 3207](https://tools.ietf.org/html/rfc3207)

## 🤝 Contributing

To add new test scenarios:
1. Create a new Python file
2. Write your test functions
3. Add it to `run_all_tests.py`
4. Update the README

## 📄 License

These test scripts share the same license as the Zetian SMTP Server project.