'use client';

import Link from 'next/link';
import { 
  Mail, 
  FileText, 
  Server,
  Calendar,
  Filter,
  Save,
  AlertTriangle,
  CheckCircle,
  Copy,
  Forward,
  Database,
  Zap
} from 'lucide-react';

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
    Console.WriteLine($"From: {message.From?.Address}");
    Console.WriteLine($"To: {string.Join(", ", message.Recipients)}");
    Console.WriteLine($"Subject: {message.Subject}");
    Console.WriteLine($"Size: {message.Size} bytes");
    Console.WriteLine($"Date: {message.Date}");
    
    // Message content
    var textBody = message.TextBody;
    var htmlBody = message.HtmlBody;
    
    // Attachments
    foreach (var attachment in message.Attachments)
    {
        Console.WriteLine($"Attachment: {attachment.FileName} ({attachment.Size} bytes)");
    }
    
    // Save the message
    var fileName = $"messages/{message.Id}.eml";
    await message.SaveToFileAsync(fileName);
};

// When message is rejected
server.MessageRejected += (sender, e) =>
{
    Console.WriteLine($"Message rejected: {e.Reason}");
    Console.WriteLine($"From: {e.From}");
    Console.WriteLine($"Recipients: {string.Join(", ", e.Recipients)}");
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
    Console.WriteLine($"Messages received: {e.Session.MessagesReceived}");
    Console.WriteLine($"Duration: {e.Session.Duration}");
};`;

const messageValidationExample = `// Message validation and filtering
server.MessageReceived += (sender, e) =>
{
    var message = e.Message;
    
    // Spam check
    if (IsSpam(message))
    {
        e.Cancel = true;
        e.Response = new SmtpResponse(550, "Message rejected: Spam detected");
        return;
    }
    
    // Virus scan
    if (ContainsVirus(message))
    {
        e.Cancel = true;
        e.Response = new SmtpResponse(550, "Message rejected: Virus detected");
        return;
    }
    
    // Size check
    if (message.Size > 10_000_000) // 10MB
    {
        e.Cancel = true;
        e.Response = new SmtpResponse(552, "Message too large");
        return;
    }
    
    // SPF/DKIM validation
    if (!ValidateSPF(e.Session.RemoteEndPoint, message.From))
    {
        e.Cancel = true;
        e.Response = new SmtpResponse(550, "SPF validation failed");
        return;
    }
    
    // Content filtering
    var blockedWords = new[] { "viagra", "lottery", "winner" };
    if (blockedWords.Any(word => 
        message.Subject?.Contains(word, StringComparison.OrdinalIgnoreCase) ?? false))
    {
        e.Cancel = true;
        e.Response = new SmtpResponse(550, "Content policy violation");
        return;
    }
};

// Dynamic rejection
server.MessageReceived += async (sender, e) =>
{
    var blacklist = await GetBlacklistAsync();
    
    if (blacklist.Contains(e.Message.From?.Address))
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
    
    // Save to database
    using var db = new SmtpDbContext();
    
    var emailEntity = new Email
    {
        Id = message.Id,
        From = message.From?.Address,
        To = string.Join(";", message.Recipients),
        Subject = message.Subject,
        TextBody = message.TextBody,
        HtmlBody = message.HtmlBody,
        Size = message.Size,
        ReceivedDate = DateTime.UtcNow,
        RemoteIp = e.Session.RemoteEndPoint?.Address.ToString(),
        RawMessage = message.GetRawMessage()
    };
    
    db.Emails.Add(emailEntity);
    
    // Save attachments
    foreach (var attachment in message.Attachments)
    {
        var attachmentEntity = new EmailAttachment
        {
            EmailId = message.Id,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            Size = attachment.Size,
            Data = attachment.GetData()
        };
        
        db.EmailAttachments.Add(attachmentEntity);
    }
    
    await db.SaveChangesAsync();
};

// Custom Message Store
public class MongoMessageStore : IMessageStore
{
    private readonly IMongoCollection<BsonDocument> _collection;
    
    public async Task<bool> SaveAsync(
        ISmtpSession session, 
        ISmtpMessage message,
        CancellationToken cancellationToken)
    {
        var document = new BsonDocument
        {
            ["_id"] = message.Id,
            ["from"] = message.From?.Address,
            ["recipients"] = new BsonArray(message.Recipients),
            ["subject"] = message.Subject,
            ["textBody"] = message.TextBody,
            ["htmlBody"] = message.HtmlBody,
            ["size"] = message.Size,
            ["receivedAt"] = DateTime.UtcNow,
            ["remoteIp"] = session.RemoteEndPoint?.Address.ToString()
        };
        
        await _collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return true;
    }
}`;

const messageForwardingExample = `// Message forwarding
server.MessageReceived += async (sender, e) =>
{
    var message = e.Message;
    
    // Forward to another SMTP server
    using var client = new SmtpClient("relay.example.com", 587);
    client.EnableSsl = true;
    client.Credentials = new NetworkCredential("relay_user", "relay_password");
    
    var mailMessage = new MailMessage
    {
        From = new MailAddress(message.From?.Address ?? "noreply@example.com"),
        Subject = message.Subject,
        Body = message.TextBody,
        IsBodyHtml = false
    };
    
    foreach (var recipient in message.Recipients)
    {
        mailMessage.To.Add(recipient);
    }
    
    // Add attachments
    foreach (var attachment in message.Attachments)
    {
        var stream = new MemoryStream(attachment.GetData());
        mailMessage.Attachments.Add(new Attachment(stream, attachment.FileName));
    }
    
    await client.SendMailAsync(mailMessage);
    
    Console.WriteLine($"Message {message.Id} forwarded to relay server");
};

// Conditional forwarding
server.MessageReceived += async (sender, e) =>
{
    var message = e.Message;
    
    // Forward messages to specific domains
    var forwardDomains = new[] { "external.com", "partner.org" };
    
    var recipientsToForward = message.Recipients
        .Where(r => forwardDomains.Any(d => r.EndsWith($"@{d}")))
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
};`;

const messageParsingExample = `// Parsing message content
server.MessageReceived += (sender, e) =>
{
    var message = e.Message;
    
    // Headers
    foreach (var header in message.Headers)
    {
        Console.WriteLine($"{header.Key}: {header.Value}");
    }
    
    // MIME parts
    foreach (var part in message.Parts)
    {
        Console.WriteLine($"Part: {part.ContentType}");
        
        if (part.IsText)
        {
            var text = part.GetText();
            Console.WriteLine($"Text content: {text.Substring(0, Math.Min(100, text.Length))}...");
        }
        else if (part.IsAttachment)
        {
            Console.WriteLine($"Attachment: {part.FileName} ({part.Size} bytes)");
        }
    }
    
    // Check custom headers
    if (message.Headers.ContainsKey("X-Priority"))
    {
        var priority = message.Headers["X-Priority"];
        if (priority == "1" || priority == "High")
        {
            // High priority message
            ProcessHighPriorityMessage(message);
        }
    }
    
    // Reply-To address
    var replyTo = message.Headers.GetValueOrDefault("Reply-To", message.From?.Address);
    
    // Message-ID
    var messageId = message.Headers.GetValueOrDefault("Message-ID", message.Id);
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
          
          <div className="relative bg-gray-900 dark:bg-gray-800 rounded-lg p-4 mb-6">
            <button
              onClick={() => copyToClipboard(messageEventsExample)}
              className="absolute top-4 right-4 p-2 hover:bg-gray-700 rounded transition-colors"
              aria-label="Copy"
            >
              <Copy className="h-4 w-4 text-gray-400" />
            </button>
            <pre className="text-gray-100 text-sm overflow-x-auto">
              <code>{messageEventsExample}</code>
            </pre>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
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
          
          <div className="relative bg-gray-900 dark:bg-gray-800 rounded-lg p-4 mb-6">
            <button
              onClick={() => copyToClipboard(messageValidationExample)}
              className="absolute top-4 right-4 p-2 hover:bg-gray-700 rounded transition-colors"
              aria-label="Copy"
            >
              <Copy className="h-4 w-4 text-gray-400" />
            </button>
            <pre className="text-gray-100 text-sm overflow-x-auto">
              <code>{messageValidationExample}</code>
            </pre>
          </div>

          <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-6">
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
          
          <div className="relative bg-gray-900 dark:bg-gray-800 rounded-lg p-4 mb-6">
            <button
              onClick={() => copyToClipboard(messageStorageExample)}
              className="absolute top-4 right-4 p-2 hover:bg-gray-700 rounded transition-colors"
              aria-label="Copy"
            >
              <Copy className="h-4 w-4 text-gray-400" />
            </button>
            <pre className="text-gray-100 text-sm overflow-x-auto">
              <code>{messageStorageExample}</code>
            </pre>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
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
          
          <div className="relative bg-gray-900 dark:bg-gray-800 rounded-lg p-4 mb-6">
            <button
              onClick={() => copyToClipboard(messageForwardingExample)}
              className="absolute top-4 right-4 p-2 hover:bg-gray-700 rounded transition-colors"
              aria-label="Copy"
            >
              <Copy className="h-4 w-4 text-gray-400" />
            </button>
            <pre className="text-gray-100 text-sm overflow-x-auto">
              <code>{messageForwardingExample}</code>
            </pre>
          </div>
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
          
          <div className="relative bg-gray-900 dark:bg-gray-800 rounded-lg p-4 mb-6">
            <button
              onClick={() => copyToClipboard(messageParsingExample)}
              className="absolute top-4 right-4 p-2 hover:bg-gray-700 rounded transition-colors"
              aria-label="Copy"
            >
              <Copy className="h-4 w-4 text-gray-400" />
            </button>
            <pre className="text-gray-100 text-sm overflow-x-auto">
              <code>{messageParsingExample}</code>
            </pre>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
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