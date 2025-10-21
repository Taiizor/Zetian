'use client';

import Link from 'next/link';
import { 
  Terminal, 
  CheckCircle, 
  ArrowRight,
  Copy,
  Package,
  Zap,
  AlertCircle
} from 'lucide-react';
import CodeBlock from '@/components/CodeBlock';

const installCommand = 'dotnet add package Zetian';
const basicExample = `using Zetian;

// Create a simple SMTP server
using var server = SmtpServerBuilder.CreateBasic();

// When message is received
server.MessageReceived += (sender, e) =>
{
    Console.WriteLine($"From: {e.Message.From}");
    Console.WriteLine($"To: {string.Join(", ", e.Message.Recipients)}");
    Console.WriteLine($"Subject: {e.Message.Subject}");
};

// Start the server
await server.StartAsync();
Console.WriteLine($"SMTP Server running on {server.Endpoint}");

// Keep the server running
Console.ReadLine();`;

const authExample = `using Zetian;

// Authenticated server
using var server = new SmtpServerBuilder()
    .Port(587)
    .RequireAuthentication()
    .SimpleAuthentication("admin", "password123")
    .Build();

// When user is authenticated
server.Authentication += (sender, e) =>
{
    Console.WriteLine($"User authenticated: {e.Username}");
};

await server.StartAsync();`;

const tlsExample = `using Zetian;

// Secure server with TLS/SSL support
using var server = new SmtpServerBuilder()
    .Port(587)
    .Certificate("certificate.pfx", "cert_password")
    .RequireSecureConnection()
    .RequireAuthentication()
    .Build();

await server.StartAsync();`;

export default function GettingStartedPage() {
  return (
    <div className="min-h-screen py-12 bg-gray-50 dark:bg-gray-950">
      <div className="container mx-auto px-4 max-w-5xl">
        {/* Header */}
        <div className="mb-12">
          <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400 mb-4">
            <Link href="/docs" className="hover:text-primary-600 dark:hover:text-primary-400">
              Documentation
            </Link>
            <span>/</span>
            <span>Getting Started</span>
          </div>
          
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
            Getting Started Guide
          </h1>
          <p className="text-xl text-gray-600 dark:text-gray-400">
            Install Zetian SMTP Server and run your first server in minutes.
          </p>
        </div>

        {/* Requirements */}
        <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-6 mb-8">
          <div className="flex items-start gap-3">
            <AlertCircle className="h-5 w-5 text-blue-600 dark:text-blue-400 mt-0.5" />
            <div>
              <h3 className="font-semibold text-blue-900 dark:text-blue-100 mb-2">Requirements</h3>
              <ul className="space-y-1 text-sm text-blue-800 dark:text-blue-200">
                <li>• .NET 6.0, 7.0, 8.0, 9.0, or 10.0</li>
                <li>• Windows, Linux, or macOS</li>
                <li>• Administrator/root privileges (for low port numbers)</li>
              </ul>
            </div>
          </div>
        </div>

        {/* Installation Steps */}
        <div className="space-y-8">
          {/* Step 1: Install */}
          <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-6">
            <div className="flex items-center gap-3 mb-4">
              <div className="w-8 h-8 bg-primary-600 text-white rounded-full flex items-center justify-center font-bold">
                1
              </div>
              <h2 className="text-2xl font-semibold text-gray-900 dark:text-white">
                Installation
              </h2>
            </div>
            
            <p className="text-gray-600 dark:text-gray-400 mb-4">
              Add Zetian to your project using NuGet Package Manager:
            </p>
            
            <CodeBlock 
              code={installCommand}
              language="bash"
              showLineNumbers={false}
            />
            
            <div className="flex items-start gap-2 text-sm text-gray-600 dark:text-gray-400">
              <Package className="h-4 w-4 mt-0.5" />
              <p>
                Alternatively, you can use Package Manager Console in Visual Studio or .NET CLI.
                <a 
                  href="https://www.nuget.org/packages/Zetian" 
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-primary-600 dark:text-primary-400 hover:underline ml-1"
                >
                  Visit NuGet page →
                </a>
              </p>
            </div>
          </div>

          {/* Step 2: Basic Server */}
          <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-6">
            <div className="flex items-center gap-3 mb-4">
              <div className="w-8 h-8 bg-primary-600 text-white rounded-full flex items-center justify-center font-bold">
                2
              </div>
              <h2 className="text-2xl font-semibold text-gray-900 dark:text-white">
                Your First SMTP Server
              </h2>
            </div>
            
            <p className="text-gray-600 dark:text-gray-400 mb-4">
              Create a simple SMTP server:
            </p>
            
            <CodeBlock 
              code={basicExample}
              language="csharp"
              filename="Program.cs"
            />
            
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mt-6">
              <div className="flex items-start gap-2">
                <CheckCircle className="h-4 w-4 text-green-500 mt-0.5" />
                <div>
                  <p className="text-sm font-medium text-gray-900 dark:text-white">Port 25</p>
                  <p className="text-xs text-gray-600 dark:text-gray-400">Default SMTP port</p>
                </div>
              </div>
              <div className="flex items-start gap-2">
                <CheckCircle className="h-4 w-4 text-green-500 mt-0.5" />
                <div>
                  <p className="text-sm font-medium text-gray-900 dark:text-white">Event-Driven</p>
                  <p className="text-xs text-gray-600 dark:text-gray-400">Listen to message events</p>
                </div>
              </div>
              <div className="flex items-start gap-2">
                <CheckCircle className="h-4 w-4 text-green-500 mt-0.5" />
                <div>
                  <p className="text-sm font-medium text-gray-900 dark:text-white">Async/Await</p>
                  <p className="text-xs text-gray-600 dark:text-gray-400">Modern async pattern</p>
                </div>
              </div>
            </div>
          </div>

          {/* Step 3: Authentication */}
          <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-6">
            <div className="flex items-center gap-3 mb-4">
              <div className="w-8 h-8 bg-primary-600 text-white rounded-full flex items-center justify-center font-bold">
                3
              </div>
              <h2 className="text-2xl font-semibold text-gray-900 dark:text-white">
                Add Authentication
              </h2>
            </div>
            
            <p className="text-gray-600 dark:text-gray-400 mb-4">
              Add authentication mechanism for security:
            </p>
            
            <CodeBlock 
              code={authExample}
              language="csharp"
              filename="Program.cs"
            />
          </div>

          {/* Step 4: TLS/SSL */}
          <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-6">
            <div className="flex items-center gap-3 mb-4">
              <div className="w-8 h-8 bg-primary-600 text-white rounded-full flex items-center justify-center font-bold">
                4
              </div>
              <h2 className="text-2xl font-semibold text-gray-900 dark:text-white">
                TLS/SSL Security
              </h2>
            </div>
            
            <p className="text-gray-600 dark:text-gray-400 mb-4">
              Add encrypted connection support with STARTTLS:
            </p>
            
            <CodeBlock 
              code={tlsExample}
              language="csharp"
              filename="Program.cs"
            />
          </div>
        </div>

        {/* Next Steps */}
        <div className="mt-12 bg-gradient-to-r from-primary-50 to-blue-50 dark:from-primary-900/20 dark:to-blue-900/20 rounded-lg border border-primary-200 dark:border-primary-800 p-6">
          <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
            🎉 Congratulations! Your first SMTP server is ready!
          </h3>
          <p className="text-gray-600 dark:text-gray-400 mb-6">
            Now you're ready to learn more advanced features:
          </p>
          
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Link 
              href="/docs/configuration"
              className="flex items-center gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg hover:shadow-md transition-all group"
            >
              <Terminal className="h-5 w-5 text-primary-600 dark:text-primary-400" />
              <div className="flex-1">
                <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-primary-600 dark:group-hover:text-primary-400">
                  Configuration
                </h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">
                  Customize server settings
                </p>
              </div>
              <ArrowRight className="h-4 w-4 text-gray-400 group-hover:text-primary-600 dark:group-hover:text-primary-400" />
            </Link>
            
            <Link 
              href="/docs/message-processing"
              className="flex items-center gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg hover:shadow-md transition-all group"
            >
              <Zap className="h-5 w-5 text-primary-600 dark:text-primary-400" />
              <div className="flex-1">
                <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-primary-600 dark:group-hover:text-primary-400">
                  Message Processing
                </h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">
                  Events and message management
                </p>
              </div>
              <ArrowRight className="h-4 w-4 text-gray-400 group-hover:text-primary-600 dark:group-hover:text-primary-400" />
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}