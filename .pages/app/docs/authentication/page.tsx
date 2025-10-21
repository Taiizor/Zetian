'use client';

import Link from 'next/link';
import { 
  Shield, 
  Lock, 
  Key, 
  UserCheck,
  AlertCircle,
  CheckCircle,
  Copy,
  Server,
  Code2,
  FileKey
} from 'lucide-react';

const basicAuthExample = `using Zetian;

// Simple username/password authentication
var server = new SmtpServerBuilder()
    .Port(587)
    .RequireAuthentication()
    .SimpleAuthentication("admin", "password123")
    .Build();

// For multiple users
var users = new Dictionary<string, string>
{
    ["admin"] = "admin123",
    ["user1"] = "pass123",
    ["demo"] = "demo123"
};

var server = new SmtpServerBuilder()
    .Port(587)
    .RequireAuthentication()
    .AuthenticationHandler(async (username, password) =>
    {
        if (users.TryGetValue(username, out var correctPassword) && 
            password == correctPassword)
        {
            return AuthenticationResult.Succeed(username);
        }
        return AuthenticationResult.Fail("Invalid credentials");
    })
    .Build();`;

const customAuthExample = `// Database authentication
public class DatabaseAuthHandler : IAuthenticationHandler
{
    private readonly IUserRepository _userRepository;
    
    public DatabaseAuthHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    
    public async Task<AuthenticationResult> AuthenticateAsync(
        string username, 
        string password,
        CancellationToken cancellationToken = default)
    {
        // Get user from database
        var user = await _userRepository.GetByUsernameAsync(username);
        
        if (user == null)
        {
            return AuthenticationResult.Fail("User not found");
        }
        
        // Check password hash
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return AuthenticationResult.Fail("Invalid password");
        }
        
        // Is account active?
        if (!user.IsActive)
        {
            return AuthenticationResult.Fail("Account is disabled");
        }
        
        // Successful authentication
        return AuthenticationResult.Succeed(username, new
        {
            UserId = user.Id,
            Email = user.Email,
            Roles = user.Roles
        });
    }
}

// Usage
var authHandler = new DatabaseAuthHandler(userRepository);
var server = new SmtpServerBuilder()
    .Port(587)
    .RequireAuthentication()
    .AuthenticationHandler(authHandler.AuthenticateAsync)
    .Build();`;

const tlsExample = `// Secure connection with TLS/SSL
var server = new SmtpServerBuilder()
    .Port(587)
    .Certificate("certificate.pfx", "certificate_password")
    .RequireSecureConnection() // TLS required
    .RequireAuthentication()   // Auth required
    .Build();

// STARTTLS support (optional TLS)
var server = new SmtpServerBuilder()
    .Port(587)
    .Certificate("certificate.pfx", "password")
    .EnableStartTls()  // STARTTLS enabled but not required
    .Build();

// SSL/TLS port (465) - Implicit TLS
var server = new SmtpServerBuilder()
    .Port(465)
    .Certificate("certificate.pfx", "password")
    .RequireSecureConnection()
    .ImplicitTls() // Encrypted from the beginning of connection
    .Build();`;

const authMechanismsExample = `// Different authentication mechanisms
var server = new SmtpServerBuilder()
    .Port(587)
    .RequireAuthentication()
    // PLAIN mechanism (default)
    .AddAuthenticator(new PlainAuthenticator(authHandler))
    // LOGIN mechanism (legacy)
    .AddAuthenticator(new LoginAuthenticator(authHandler))
    // CRAM-MD5 (optional)
    .AddAuthenticator(new CramMd5Authenticator(authHandler))
    .Build();

// Auth events
server.Authentication += (sender, e) =>
{
    if (e.IsAuthenticated)
    {
        Console.WriteLine($"User authenticated: {e.Username}");
        // Login record, audit log, etc.
    }
    else
    {
        Console.WriteLine($"Authentication failed for: {e.Username}");
        // Failed login record, security alert
    }
};

server.SessionCreated += (sender, e) =>
{
    if (e.Session.AuthenticatedUser != null)
    {
        Console.WriteLine($"Authenticated session: {e.Session.AuthenticatedUser}");
    }
};`;

const authFlowExample = `// Authentication flow
// 1. Client: Sends AUTH PLAIN command
// 2. Server: Continues with 334 response
// 3. Client: Sends Base64 encoded credentials
// 4. Server: Validates and responds

// Example SMTP session:
C: EHLO client.example.com
S: 250-smtp.example.com
S: 250-AUTH PLAIN LOGIN
S: 250 STARTTLS

C: STARTTLS
S: 220 Ready to start TLS
[TLS handshake]

C: EHLO client.example.com
S: 250-smtp.example.com
S: 250 AUTH PLAIN LOGIN

C: AUTH PLAIN
S: 334
C: AGFkbWluAHBhc3N3b3JkMTIz
S: 235 Authentication successful

// Now can send mail
C: MAIL FROM:<sender@example.com>
S: 250 OK`;

export default function AuthenticationPage() {
  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
  };

  return (
    <div className="min-h-screen py-12 bg-gray-50 dark:bg-gray-950">
      <div className="container mx-auto px-4 max-w-5xl">
        {/* Header */}
        <div className="mb-12">
          <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400 mb-4">
            <Link href="/docs" className="hover:text-blue-600 dark:hover:text-blue-400">
              Documentation
            </Link>
            <span>/</span>
            <span>Authentication</span>
          </div>
          
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
            Authentication and Security
          </h1>
          <p className="text-xl text-gray-600 dark:text-gray-400">
            Make your SMTP server secure and add user authentication.
          </p>
        </div>

        {/* Security Overview */}
        <div className="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-6 mb-8">
          <div className="flex items-start gap-3">
            <Shield className="h-5 w-5 text-green-600 dark:text-green-400 mt-0.5" />
            <div>
              <h3 className="font-semibold text-green-900 dark:text-green-100 mb-2">Security is Top Priority</h3>
              <p className="text-sm text-green-800 dark:text-green-200">
                Zetian supports modern security standards: TLS 1.2/1.3, STARTTLS,
                multiple authentication mechanisms and customizable auth handlers.
              </p>
            </div>
          </div>
        </div>

        {/* Basic Authentication */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <UserCheck className="h-6 w-6" />
            Basic Authentication
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Authentication with simple username and password:
          </p>
          
          <div className="relative bg-gray-900 dark:bg-gray-800 rounded-lg p-4 mb-6">
            <button
              onClick={() => copyToClipboard(basicAuthExample)}
              className="absolute top-4 right-4 p-2 hover:bg-gray-700 rounded transition-colors"
              aria-label="Copy"
            >
              <Copy className="h-4 w-4 text-gray-400" />
            </button>
            <pre className="text-gray-100 text-sm overflow-x-auto">
              <code>{basicAuthExample}</code>
            </pre>
          </div>
        </section>

        {/* Custom Authentication */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Key className="h-6 w-6" />
            Custom Authentication
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Integration with database or external systems:
          </p>
          
          <div className="relative bg-gray-900 dark:bg-gray-800 rounded-lg p-4 mb-6">
            <button
              onClick={() => copyToClipboard(customAuthExample)}
              className="absolute top-4 right-4 p-2 hover:bg-gray-700 rounded transition-colors"
              aria-label="Copy"
            >
              <Copy className="h-4 w-4 text-gray-400" />
            </button>
            <pre className="text-gray-100 text-sm overflow-x-auto">
              <code>{customAuthExample}</code>
            </pre>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-4">
              <CheckCircle className="h-5 w-5 text-green-500 mb-2" />
              <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Database Integration</h4>
              <p className="text-sm text-gray-600 dark:text-gray-400">
                SQL, NoSQL or any data source
              </p>
            </div>
            
            <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-4">
              <CheckCircle className="h-5 w-5 text-green-500 mb-2" />
              <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Password Hashing</h4>
              <p className="text-sm text-gray-600 dark:text-gray-400">
                BCrypt, Argon2 or other hash algorithms
              </p>
            </div>
            
            <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-4">
              <CheckCircle className="h-5 w-5 text-green-500 mb-2" />
              <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Account Status</h4>
              <p className="text-sm text-gray-600 dark:text-gray-400">
                Active/passive account check, role-based access
              </p>
            </div>
          </div>
        </section>

        {/* TLS/SSL Configuration */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Lock className="h-6 w-6" />
            TLS/SSL Security
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            TLS/SSL configuration for encrypted connections:
          </p>
          
          <div className="relative bg-gray-900 dark:bg-gray-800 rounded-lg p-4 mb-6">
            <button
              onClick={() => copyToClipboard(tlsExample)}
              className="absolute top-4 right-4 p-2 hover:bg-gray-700 rounded transition-colors"
              aria-label="Copy"
            >
              <Copy className="h-4 w-4 text-gray-400" />
            </button>
            <pre className="text-gray-100 text-sm overflow-x-auto">
              <code>{tlsExample}</code>
            </pre>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
              <h4 className="font-semibold text-blue-900 dark:text-blue-100 mb-2">STARTTLS (Port 587)</h4>
              <p className="text-sm text-blue-800 dark:text-blue-200 mb-2">
                Initially plain text, transition to encryption with STARTTLS command.
              </p>
              <ul className="space-y-1 text-xs text-blue-700 dark:text-blue-300">
                <li>• Recommended for modern email clients</li>
                <li>• Backward compatible</li>
                <li>• Flexible security</li>
              </ul>
            </div>
            
            <div className="bg-purple-50 dark:bg-purple-900/20 border border-purple-200 dark:border-purple-800 rounded-lg p-4">
              <h4 className="font-semibold text-purple-900 dark:text-purple-100 mb-2">Implicit TLS (Port 465)</h4>
              <p className="text-sm text-purple-800 dark:text-purple-200 mb-2">
                Fully encrypted from the beginning of connection.
              </p>
              <ul className="space-y-1 text-xs text-purple-700 dark:text-purple-300">
                <li>• Maximum security</li>
                <li>• Old standard but still in use</li>
                <li>• Known as SMTPS</li>
              </ul>
            </div>
          </div>
        </section>

        {/* Authentication Mechanisms */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Server className="h-6 w-6" />
            Authentication Mechanisms
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Supported authentication mechanisms and events:
          </p>
          
          <div className="relative bg-gray-900 dark:bg-gray-800 rounded-lg p-4 mb-6">
            <button
              onClick={() => copyToClipboard(authMechanismsExample)}
              className="absolute top-4 right-4 p-2 hover:bg-gray-700 rounded transition-colors"
              aria-label="Copy"
            >
              <Copy className="h-4 w-4 text-gray-400" />
            </button>
            <pre className="text-gray-100 text-sm overflow-x-auto">
              <code>{authMechanismsExample}</code>
            </pre>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="flex items-start gap-2">
              <FileKey className="h-4 w-4 text-blue-500 mt-0.5" />
              <div>
                <p className="text-sm font-medium text-gray-900 dark:text-white">PLAIN</p>
                <p className="text-xs text-gray-600 dark:text-gray-400">
                  Base64 encoded username and password
                </p>
              </div>
            </div>
            <div className="flex items-start gap-2">
              <FileKey className="h-4 w-4 text-green-500 mt-0.5" />
              <div>
                <p className="text-sm font-medium text-gray-900 dark:text-white">LOGIN</p>
                <p className="text-xs text-gray-600 dark:text-gray-400">
                  Legacy type, Microsoft Outlook compatible
                </p>
              </div>
            </div>
            <div className="flex items-start gap-2">
              <FileKey className="h-4 w-4 text-purple-500 mt-0.5" />
              <div>
                <p className="text-sm font-medium text-gray-900 dark:text-white">CRAM-MD5</p>
                <p className="text-xs text-gray-600 dark:text-gray-400">
                  Challenge-response based (optional)
                </p>
              </div>
            </div>
          </div>
        </section>

        {/* SMTP Auth Flow */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Code2 className="h-6 w-6" />
            SMTP Authentication Flow
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            A typical SMTP authentication session:
          </p>
          
          <div className="relative bg-gray-900 dark:bg-gray-800 rounded-lg p-4 mb-6">
            <button
              onClick={() => copyToClipboard(authFlowExample)}
              className="absolute top-4 right-4 p-2 hover:bg-gray-700 rounded transition-colors"
              aria-label="Copy"
            >
              <Copy className="h-4 w-4 text-gray-400" />
            </button>
            <pre className="text-gray-100 text-sm overflow-x-auto">
              <code>{authFlowExample}</code>
            </pre>
          </div>

          <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-6">
            <div className="flex items-start gap-3">
              <AlertCircle className="h-5 w-5 text-yellow-600 dark:text-yellow-400 mt-0.5" />
              <div>
                <h4 className="font-semibold text-yellow-900 dark:text-yellow-100 mb-2">Security Tip</h4>
                <p className="text-sm text-yellow-800 dark:text-yellow-200">
                  PLAIN and LOGIN mechanisms encode passwords with Base64 but do not encrypt.
                  Therefore, they must be used with TLS/SSL.
                </p>
              </div>
            </div>
          </div>
        </section>

        {/* Next Steps */}
        <div className="mt-12 flex gap-4">
          <Link 
            href="/docs/message-processing"
            className="flex-1 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 hover:shadow-md transition-all group"
          >
            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">
                  Message Processing →
                </h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                  Message receiving and processing events
                </p>
              </div>
              <Server className="h-5 w-5 text-gray-400" />
            </div>
          </Link>
        </div>
      </div>
    </div>
  );
}