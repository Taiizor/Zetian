# Zetian SMTP Server Project Structure

## 📁 Proposed Professional Folder Organization

```
src/Zetian/
│
├── 📁 Abstractions/              # Interfaces & Abstract Classes
│   ├── ISmtpServer.cs           # Main server interface
│   ├── ISmtpSession.cs          # Session interface
│   ├── ISmtpMessage.cs          # Message interface
│   ├── IAuthenticator.cs        # Authentication interface
│   ├── IMessageStore.cs         # Storage interface
│   ├── IMailboxFilter.cs        # Mailbox filtering interface
│   ├── IRateLimiter.cs          # Rate limiting interface
│   └── IStatisticsCollector.cs  # Statistics interface
│
├── 📁 Models/                    # Data Models & DTOs
│   ├── EventArgs/               # Event argument models
│   │   ├── SessionEventArgs.cs
│   │   ├── MessageEventArgs.cs
│   │   └── AuthenticationEventArgs.cs
│   ├── SmtpResponse.cs
│   ├── SmtpCommand.cs
│   └── MailAddress.cs
│
├── 📁 Configuration/             # Configuration Classes
│   └── SmtpServerConfiguration.cs
│
├── 📁 Server/                    # Core Server Implementation
│   ├── SmtpServer.cs            # Main server class
│   └── SmtpServerBuilder.cs     # Builder pattern implementation
│
├── 📁 Session/                   # Session Management
│   ├── SmtpSession.cs
│   └── SessionManager.cs
│
├── 📁 Protocol/                  # SMTP Protocol Implementation
│   ├── Commands/                # SMTP commands
│   └── Responses/               # SMTP responses
│
├── 📁 Authentication/            # Authentication Mechanisms
│   ├── PlainAuthenticator.cs
│   ├── LoginAuthenticator.cs
│   ├── CramMd5Authenticator.cs
│   └── AuthenticationManager.cs
│
├── 📁 Storage/                   # Message Storage
│   ├── InMemoryMessageStore.cs
│   ├── FileSystemMessageStore.cs
│   ├── NullMessageStore.cs
│   ├── DelegateMessageStore.cs
│   ├── CompositeMessageStore.cs
│   ├── MailboxFilterCollection.cs
│   └── DnsMailboxFilter.cs
│
├── 📁 Extensions/                # Extension Methods
│   ├── SmtpServerExtensions.cs
│   ├── ServiceProviderExtensions.cs
│   ├── TaskExtensions.cs
│   └── StringExtensions.cs
│
├── 📁 RateLimiting/             # Rate Limiting
│   ├── FixedWindowRateLimiter.cs
│   ├── SlidingWindowRateLimiter.cs
│   └── TokenBucketRateLimiter.cs
│
├── 📁 Internal/                  # Internal Implementation Details
│   ├── SmtpSessionContext.cs
│   ├── SmtpMessageTransaction.cs
│   └── NetworkStreamReader.cs
│
├── 📁 Properties/                # Assembly Properties
│
├── 📁 Resources/                 # Resource Files
│
├── Zetian.csproj                # Project File
└── README.md                     # Project Documentation
```

## 🏗️ Architecture Overview

### Abstractions Layer
- **Purpose**: Define all contracts and interfaces
- **Benefits**: Clean separation, dependency inversion, testability
- All interfaces in one place for easy discovery

### Models Layer
- **Purpose**: Data transfer objects and domain models
- **EventArgs**: All event-related models in a subfolder
- No business logic, just data structures

### Server Layer
- **Purpose**: Core server implementation
- **SmtpServer**: Main server class with all the logic
- **SmtpServerBuilder**: Fluent API for server configuration

### Configuration Layer
- **Purpose**: All configuration-related classes
- Follows Options pattern
- Immutable configuration objects

### Session Layer
- **Purpose**: Session lifecycle management
- Session creation, tracking, and disposal
- Connection handling

### Protocol Layer
- **Purpose**: SMTP protocol implementation
- Command parsing and execution
- Response generation

### Authentication Layer
- **Purpose**: Various authentication mechanisms
- Pluggable authentication providers
- Support for PLAIN, LOGIN, CRAM-MD5, etc.

### Storage Layer
- **Purpose**: Message persistence
- Multiple storage backends
- Mailbox filtering logic

### RateLimiting Layer
- **Purpose**: Request throttling
- Different rate limiting algorithms
- Per-IP, per-user limiting

### Extensions Layer
- **Purpose**: Extension methods and helpers
- Fluent API extensions
- Utility methods

### Internal Layer
- **Purpose**: Implementation details
- Not part of public API
- Helper classes and utilities

## 📝 Namespace Organization

```csharp
// Core abstractions
namespace Zetian.Abstractions

// Data models
namespace Zetian.Models
namespace Zetian.Models.EventArgs

// Server implementation
namespace Zetian.Server

// Configuration
namespace Zetian.Configuration

// Session management
namespace Zetian.Session

// Protocol implementation
namespace Zetian.Protocol
namespace Zetian.Protocol.Commands
namespace Zetian.Protocol.Responses

// Authentication
namespace Zetian.Authentication

// Storage
namespace Zetian.Storage

// Rate limiting
namespace Zetian.RateLimiting

// Extensions
namespace Zetian.Extensions

// Internal utilities
namespace Zetian.Internal
```

## 🎯 Design Principles

1. **Single Responsibility**: Each class has one reason to change
2. **Open/Closed**: Open for extension, closed for modification
3. **Liskov Substitution**: Derived classes can substitute base classes
4. **Interface Segregation**: Many specific interfaces over few general ones
5. **Dependency Inversion**: Depend on abstractions, not concretions
6. **DRY (Don't Repeat Yourself)**: No code duplication
7. **KISS (Keep It Simple)**: Simple solutions over complex ones
8. **YAGNI (You Aren't Gonna Need It)**: Only implement what's needed

## 🔧 Migration Plan

### Phase 1: Create New Structure
1. Create all new folders
2. Don't delete anything yet

### Phase 2: Move Interfaces
1. Move all interfaces to `Abstractions/`
2. Update namespaces to `Zetian.Abstractions`

### Phase 3: Move Models
1. Move EventArgs to `Models/EventArgs/`
2. Create other model classes

### Phase 4: Reorganize Core Classes
1. Move SmtpServer.cs to `Server/`
2. Move SmtpServerBuilder.cs to `Server/`

### Phase 5: Update References
1. Update all using statements
2. Fix namespace references
3. Run tests to ensure everything works

### Phase 6: Cleanup
1. Remove old empty folders
2. Update documentation

## 🚀 Benefits of This Structure

- ✅ **Discoverability**: Easy to find what you're looking for
- ✅ **Maintainability**: Clear organization reduces cognitive load
- ✅ **Scalability**: Easy to add new features in the right place
- ✅ **Testability**: Clean separation enables unit testing
- ✅ **Onboarding**: New developers understand the structure quickly
- ✅ **Dependency Management**: Clear dependency flow
- ✅ **Code Reuse**: Interfaces enable multiple implementations
- ✅ **Professional**: Follows industry best practices

## 📊 Comparison with Current Structure

### Current Issues:
- ❌ Interfaces scattered across multiple folders
- ❌ Core classes in root directory
- ❌ Mixed responsibilities in Core folder
- ❌ Rate limiting inside Extensions folder

### New Structure Advantages:
- ✅ All interfaces in Abstractions folder
- ✅ Clear separation of concerns
- ✅ Rate limiting as first-class feature
- ✅ Dedicated folders for each responsibility

## 🛠️ Usage Example

```csharp
using Zetian.Abstractions;
using Zetian.Server;
using Zetian.Configuration;
using Zetian.Storage;
using Zetian.Authentication;

// Clean imports from organized namespaces
var config = new SmtpServerConfiguration
{
    Port = 25,
    ServerName = "mail.example.com"
};

var builder = new SmtpServerBuilder()
    .UseConfiguration(config)
    .UseAuthentication<PlainAuthenticator>()
    .UseStorage<FileSystemMessageStore>()
    .UseRateLimiting<TokenBucketRateLimiter>();

ISmtpServer server = builder.Build();
await server.StartAsync();
```