'use client';

import Link from 'next/link';
import { 
  Filter, 
  Database, 
  CheckCircle,
  FileText,
  Zap,
  Calendar,
  AlertTriangle,
  Save,
  Forward,
  Server
} from 'lucide-react';
import CodeBlock from '@/components/CodeBlock';

const messageEventsExample = `using Zetian;

var server = new SmtpServerBuilder()
    .Port(25)
    .Build();

// When message is received
server.MessageReceived += async (sender, e) =>
{
    var message = e.Message;
    
    // Message information
    Console.WriteLine($"Message ID: {message.Id}");
    Console.WriteLine($"From: {message.From}");
    Console.WriteLine($"To: {string.Join(", ", message.Recipients)}");
    Console.WriteLine($"Size: {message.Size} bytes");
    Console.WriteLine($"Subject: {message.Subject}");
    
    // Session information
    Console.WriteLine($"Session: {e.Session.Id}");
    Console.WriteLine($"From IP: {e.Session.RemoteEndPoint}");
    Console.WriteLine($"Is authenticated: {e.Session.IsAuthenticated}");
    
    // Save the raw message
    var fileName = $"messages/{message.Id}.eml";
    Directory.CreateDirectory("messages");
    await message.SaveToFileAsync(fileName);
    
    // Reject message if needed
    if (message.From?.Address?.Contains("spam") == true)
    {
        e.Cancel = true;
        e.Response = new SmtpResponse(554, "Message rejected as spam");
    }
};

// When session is created
server.SessionCreated += (sender, e) =>
{
    Console.WriteLine($"New session from {e.Session.RemoteEndPoint}");
    Console.WriteLine($"Session ID: {e.Session.Id}");
};

// When session is completed
server.SessionCompleted += (sender, e) =>
{
    Console.WriteLine($"Session completed: {e.Session.Id}");
    Console.WriteLine($"Messages received: {e.Session.MessageCount}");
    
    // Calculate duration
    var duration = DateTime.UtcNow - e.Session.StartTime;
    Console.WriteLine($"Duration: {duration.TotalSeconds:F2} seconds");
    
    if (e.Session.IsAuthenticated)
    {
        Console.WriteLine($"Authenticated as: {e.Session.AuthenticatedIdentity}");
    }
};`;

const messageValidationExample = `// Message validation and filtering
server.MessageReceived += (sender, e) =>
{
    var message = e.Message;
    
    // Size check
    if (message.Size > 10_000_000) // 10MB
    {
        e.Cancel = true;
        e.Response = new SmtpResponse(552, "Message too large");
        return;
    }
    
    // Check sender domain
    if (message.From?.Address?.Contains("@spammer.com") == true)
    {
        e.Cancel = true;
        e.Response = new SmtpResponse(550, "Sender domain blocked");
        return;
    }
    
    // SPF/DKIM validation
    if (!ValidateSPF(e.Session.RemoteEndPoint, message.From?.Address))
    {
        e.Cancel = true;
        e.Response = new SmtpResponse(550, "SPF validation failed");
        return;
    }
    
    // Parse with MimeKit for content filtering
    using var stream = new MemoryStream(message.GetRawData());
    var mimeMessage = MimeMessage.Load(stream);
    
    // Content filtering
    var blockedWords = new[] { "viagra", "lottery", "winner" };
    if (blockedWords.Any(word => 
        mimeMessage.Subject?.Contains(word, StringComparison.OrdinalIgnoreCase) ?? false))
    {
        e.Cancel = true;
        e.Response = new SmtpResponse(550, "Content policy violation");
        return;
    }
    
    // Virus scan (example with external service)
    if (await ScanForVirus(message.GetRawData()))
    {
        e.Cancel = true;
        e.Response = new SmtpResponse(550, "Message rejected: Virus detected");
        return;
    }
};

// Dynamic rejection
server.MessageReceived += async (sender, e) =>
{
    var blacklist = await GetBlacklistAsync();
    
    if (blacklist.Contains(e.Message.From))
    {
        e.Cancel = true;
        e.Response = new SmtpResponse(550, "Sender blacklisted");
    }
};`;

const messageStorageExample = `// Protocol-Level Storage (with SmtpServerBuilder)
var server = new SmtpServerBuilder()
    .Port(25)
    .WithFileMessageStore(@"C:\\smtp_messages", createDateFolders: true)
    .Build();

// Event-Based Storage (with Event handler)
server.MessageReceived += async (sender, e) =>
{
    var message = e.Message;
    
    // Save to file
    var directory = $"messages/{DateTime.Now:yyyy-MM-dd}";
    Directory.CreateDirectory(directory);
    
    var fileName = $"{directory}/{message.Id}.eml";
    await message.SaveToFileAsync(fileName);
    
    // Save message metadata as JSON (no external dependencies needed)
    var messageInfo = new
    {
        Id = message.Id,
        From = message.From?.Address,
        To = message.Recipients.Select(r => r.Address).ToArray(),
        Size = message.Size,
        Subject = message.Subject,
        ReceivedDate = DateTime.UtcNow,
        RemoteIp = e.Session.RemoteEndPoint?.ToString(),
        HasAttachments = message.HasAttachments,
        AttachmentCount = message.AttachmentCount
    };
    
    var jsonFile = $"{directory}/{message.Id}.json";
    var json = JsonSerializer.Serialize(messageInfo, new JsonSerializerOptions 
    { 
        WriteIndented = true 
    });
    await File.WriteAllTextAsync(jsonFile, json);
};

// Custom Message Store Implementation
public class JsonMessageStore : IMessageStore
{
    private readonly string _directory;
    
    public JsonMessageStore(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(directory);
    }
    
    public async Task<bool> SaveAsync(
        ISmtpSession session, 
        ISmtpMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            // Save raw message
            var emlFile = Path.Combine(_directory, $"{message.Id}.eml");
            await message.SaveToFileAsync(emlFile);
            
            // Save metadata as JSON
            var metadata = new
            {
                Id = message.Id,
                From = message.From?.Address,
                Recipients = message.Recipients.Select(r => r.Address).ToArray(),
                Subject = message.Subject,
                Size = message.Size,
                ReceivedAt = DateTime.UtcNow,
                SessionId = session.Id,
                RemoteEndPoint = session.RemoteEndPoint?.ToString(),
                IsAuthenticated = session.IsAuthenticated,
                AuthenticatedUser = session.AuthenticatedIdentity
            };
            
            var jsonFile = Path.Combine(_directory, $"{message.Id}.json");
            var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            await File.WriteAllTextAsync(jsonFile, json, cancellationToken);
            
            return true;
        }
        catch
        {
            return false;
        }
    }
}`;

const messageForwardingExample = `// Simple message forwarding
server.MessageReceived += async (sender, e) =>
{
    var message = e.Message;
    
    try
    {
        // Forward to another SMTP server
        using var client = new SmtpClient("relay.example.com", 587);
        client.EnableSsl = true;
        client.Credentials = new NetworkCredential("relay_user", "relay_password");
        
        var mailMessage = new MailMessage
        {
            From = new MailAddress(message.From?.Address ?? "noreply@example.com"),
            Subject = message.Subject ?? "(No Subject)",
            Body = message.TextBody ?? string.Empty,
            IsBodyHtml = false
        };
        
        foreach (var recipient in message.Recipients)
        {
            mailMessage.To.Add(recipient.Address);
        }
        
        // Note: To handle attachments, parse the raw message with MimeKit:
        // var mimeMessage = MimeMessage.Load(new MemoryStream(message.GetRawData()));
        // foreach (var attachment in mimeMessage.Attachments) { ... }
        
        await client.SendMailAsync(mailMessage);
        Console.WriteLine($"Message {message.Id} forwarded to relay server");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to forward message: {ex.Message}");
    }
};

// Conditional forwarding based on recipient domains
server.MessageReceived += async (sender, e) =>
{
    var message = e.Message;
    
    // Forward messages to specific domains
    var forwardDomains = new[] { "external.com", "partner.org" };
    
    var recipientsToForward = message.Recipients
        .Where(r => forwardDomains.Any(d => r.Address.EndsWith($"@{d}", StringComparison.OrdinalIgnoreCase)))
        .ToList();
    
    if (recipientsToForward.Any())
    {
        await ForwardToExternalServer(message, recipientsToForward);
    }
    
    // Process the rest locally
    var localRecipients = message.Recipients
        .Except(recipientsToForward)
        .ToList();
    
    if (localRecipients.Any())
    {
        await ProcessLocally(message, localRecipients);
    }
};

// Helper method to forward messages to external server
private static async Task ForwardToExternalServer(ISmtpMessage message, List<MailAddress> recipients)
{
    using var client = new SmtpClient("external-relay.example.com", 587);
    client.EnableSsl = true;
    client.Credentials = new NetworkCredential("external_user", "external_password");

    var mailMessage = new MailMessage
    {
        From = new MailAddress(message.From?.Address ?? "noreply@example.com"),
        Subject = message.Subject ?? "(No Subject)",
        Body = message.TextBody ?? string.Empty,
        IsBodyHtml = false
    };

    foreach (var recipient in recipients)
    {
        mailMessage.To.Add(recipient.Address);
    }

    await client.SendMailAsync(mailMessage);
    Console.WriteLine($"Forwarded to external server for {recipients.Count} recipient(s)");
}

// Helper method to process messages locally  
private static async Task ProcessLocally(ISmtpMessage message, List<MailAddress> recipients)
{
    foreach (var recipient in recipients)
    {
        var mailboxDir = $"mailboxes/{recipient.Address.Replace("@", "_at_")}";
        Directory.CreateDirectory(mailboxDir);
        
        var fileName = Path.Combine(mailboxDir, $"{message.Id}.eml");
        await message.SaveToFileAsync(fileName);
        
        Console.WriteLine($"Message saved to local mailbox: {recipient.Address}");
    }
}`;

const messageParsingExample = `// Parsing message content with MimeKit
// Install-Package MimeKit
using MimeKit;

server.MessageReceived += (sender, e) =>
{
    var message = e.Message;
    
    // Parse raw message with MimeKit
    using var stream = new MemoryStream(message.GetRawData());
    var mimeMessage = MimeMessage.Load(stream);
    
    // Headers
    foreach (var header in mimeMessage.Headers)
    {
        Console.WriteLine($"{header.Field}: {header.Value}");
    }
    
    // Basic properties
    Console.WriteLine($"Subject: {mimeMessage.Subject}");
    Console.WriteLine($"From: {mimeMessage.From}");
    Console.WriteLine($"To: {mimeMessage.To}");
    Console.WriteLine($"Date: {mimeMessage.Date}");
    
    // Text and HTML body
    var textBody = mimeMessage.TextBody;
    var htmlBody = mimeMessage.HtmlBody;
    
    // Attachments
    foreach (var attachment in mimeMessage.Attachments)
    {
        if (attachment is MimePart part)
        {
            Console.WriteLine($"Attachment: {part.FileName} ({part.ContentType})");
            
            // Save attachment
            using var attachmentStream = File.Create($"attachments/{part.FileName}");
            part.Content.DecodeTo(attachmentStream);
        }
    }
    
    // Priority header
    if (mimeMessage.Headers.Contains(HeaderId.XPriority))
    {
        var priority = mimeMessage.Headers[HeaderId.XPriority];
        Console.WriteLine($"Priority: {priority}");
    }
};`;

export default function MessageProcessingPage() {
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
            <span>Message Processing</span>
          </div>
          
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
            Message Processing
          </h1>
          <p className="text-xl text-gray-600 dark:text-gray-400">
            Receiving, processing, validating and storing SMTP messages.
          </p>
        </div>

        {/* Message Events */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Calendar className="h-6 w-6" />
            Message Events
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            SMTP server triggers various events:
          </p>
          
          <CodeBlock 
            code={messageEventsExample}
            language="csharp"
            filename="MessageEvents.cs"
          />

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-6">
            <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-4">
              <h4 className="font-semibold text-gray-900 dark:text-white mb-2">Message Events</h4>
              <ul className="space-y-1 text-sm text-gray-600 dark:text-gray-400">
                <li>• <code className="text-blue-600 dark:text-blue-400">MessageReceived</code> - Message received</li>
                <li>• <code className="text-blue-600 dark:text-blue-400">MessageRejected</code> - Message rejected</li>
                <li>• <code className="text-blue-600 dark:text-blue-400">MessageStored</code> - Message stored</li>
                <li>• <code className="text-blue-600 dark:text-blue-400">MessageForwarded</code> - Message forwarded</li>
              </ul>
            </div>
            
            <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-4">
              <h4 className="font-semibold text-gray-900 dark:text-white mb-2">Session Events</h4>
              <ul className="space-y-1 text-sm text-gray-600 dark:text-gray-400">
                <li>• <code className="text-green-600 dark:text-green-400">SessionCreated</code> - Session started</li>
                <li>• <code className="text-green-600 dark:text-green-400">SessionCompleted</code> - Session completed</li>
                <li>• <code className="text-green-600 dark:text-green-400">Authentication</code> - Authenticated</li>
                <li>• <code className="text-green-600 dark:text-green-400">ErrorOccurred</code> - Error occurred</li>
              </ul>
            </div>
          </div>
        </section>

        {/* Message Validation */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Filter className="h-6 w-6" />
            Message Validation and Filtering
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Validate messages and filter unwanted content:
          </p>
          
          <CodeBlock 
            code={messageValidationExample}
            language="csharp"
            filename="MessageValidation.cs"
          />

          <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-6 mt-6">
            <div className="flex items-start gap-3">
              <AlertTriangle className="h-5 w-5 text-yellow-600 dark:text-yellow-400 mt-0.5" />
              <div>
                <h4 className="font-semibold text-yellow-900 dark:text-yellow-100 mb-2">Performance Tip</h4>
                <p className="text-sm text-yellow-800 dark:text-yellow-200">
                  Protocol-level filtering (with SmtpServerBuilder) is more performant as messages are rejected before being fully received.
                  Event-based filtering is more flexible but runs after the entire message is received.
                </p>
              </div>
            </div>
          </div>
        </section>

        {/* Message Storage */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Database className="h-6 w-6" />
            Message Storage
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Save messages to file system or database:
          </p>
          
          <CodeBlock 
            code={messageStorageExample}
            language="csharp"
            filename="MessageStorage.cs"
          />

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mt-6">
            <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-4">
              <Save className="h-5 w-5 text-blue-500 mb-2" />
              <h4 className="font-semibold text-gray-900 dark:text-white mb-1">File System</h4>
              <p className="text-sm text-gray-600 dark:text-gray-400">
                Save as EML format files
              </p>
            </div>
            
            <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-4">
              <Database className="h-5 w-5 text-green-500 mb-2" />
              <h4 className="font-semibold text-gray-900 dark:text-white mb-1">SQL/NoSQL</h4>
              <p className="text-sm text-gray-600 dark:text-gray-400">
                Entity Framework, MongoDB, etc.
              </p>
            </div>
            
            <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-4">
              <Zap className="h-5 w-5 text-purple-500 mb-2" />
              <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Cloud Storage</h4>
              <p className="text-sm text-gray-600 dark:text-gray-400">
                Azure Blob, AWS S3, etc.
              </p>
            </div>
          </div>
        </section>

        {/* Message Forwarding */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Forward className="h-6 w-6" />
            Message Forwarding
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Forward messages to other servers or systems:
          </p>
          
          <CodeBlock 
            code={messageForwardingExample}
            language="csharp"
            filename="MessageForwarding.cs"
          />
        </section>

        {/* Message Parsing */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <FileText className="h-6 w-6" />
            Parsing Message Content
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Process MIME parts, headers and attachments:
          </p>
          
          <CodeBlock 
            code={messageParsingExample}
            language="csharp"
            filename="MessageParsing.cs"
          />

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-6">
            <div className="flex items-start gap-2">
              <CheckCircle className="h-4 w-4 text-green-500 mt-0.5" />
              <div>
                <p className="text-sm font-medium text-gray-900 dark:text-white">Headers</p>
                <p className="text-xs text-gray-600 dark:text-gray-400">
                  From, To, Subject, Date, Message-ID, Custom headers
                </p>
              </div>
            </div>
            <div className="flex items-start gap-2">
              <CheckCircle className="h-4 w-4 text-green-500 mt-0.5" />
              <div>
                <p className="text-sm font-medium text-gray-900 dark:text-white">MIME Parts</p>
                <p className="text-xs text-gray-600 dark:text-gray-400">
                  Text/plain, text/html, multipart, attachments
                </p>
              </div>
            </div>
          </div>
        </section>

        {/* Next Steps */}
        <div className="mt-12 flex gap-4">
          <Link 
            href="/docs/extensions"
            className="flex-1 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 hover:shadow-md transition-all group"
          >
            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">
                  Extensions →
                </h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                  Plugin and extension development
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