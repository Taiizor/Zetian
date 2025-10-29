# Zetian.AntiSpam - Advanced Spam Protection for SMTP

[![NuGet-Version](https://img.shields.io/nuget/v/Zetian.AntiSpam.svg?label=NuGet)](https://www.nuget.org/packages/Zetian.AntiSpam)
[![NuGet-Download](https://img.shields.io/nuget/dt/Zetian.AntiSpam?label=Download)](https://www.nuget.org/api/v2/package/Zetian.AntiSpam)
[![License](https://img.shields.io/github/license/Taiizor/Zetian.svg?label=License)](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

Comprehensive anti-spam solution for Zetian SMTP Server with multiple detection methods, machine learning, and flexible configuration options.

## ⚡ Features

- 📊 **Statistics** - Detailed metrics and reporting
- 🚫 **RBL/DNSBL** - Realtime blackhole list checking
- 🔧 **Extensible** - Easy to add custom spam checkers
- 📧 **DKIM/DMARC** - Email authentication protocols
- 🛡️ **SPF Validation** - Sender Policy Framework verification
- ⏱️ **Greylisting** - Temporary rejection for unknown senders
- ⚡ **High Performance** - Parallel checking with timeout support
- 🤖 **Bayesian Filtering** - Machine learning-based spam detection

## 📦 Installation

```bash
# Install Zetian SMTP Server (required)
dotnet add package Zetian

# Install AntiSpam Extension
dotnet add package Zetian.AntiSpam
```

## 🚀 Quick Start

### Basic Anti-Spam Setup

```csharp
using Zetian.Server;
using Zetian.AntiSpam.Extensions;

// Create SMTP server with anti-spam
var server = SmtpServerBuilder.CreateBasic();

// Add anti-spam with default settings
server.AddAntiSpam();

await server.StartAsync();
```

### Custom Configuration

```csharp
// Configure anti-spam features
server.AddAntiSpam(builder => builder
    .EnableSpf(failScore: 50)
    .EnableRbl("zen.spamhaus.org", "bl.spamcop.net")
    .EnableBayesian(spamThreshold: 0.85)
    .EnableGreylisting(initialDelay: TimeSpan.FromMinutes(5))
    .WithOptions(options =>
    {
        options.RejectThreshold = 60;
        options.TempFailThreshold = 40;
        options.RunChecksInParallel = true;
    }));
```

### Aggressive Protection

```csharp
// Use aggressive anti-spam settings
server.AddAntiSpam(builder => builder.UseAggressive());
```

### Lenient Protection

```csharp
// Use lenient settings for trusted environments
server.AddAntiSpam(builder => builder.UseLenient());
```

## 🎯 Individual Features

### SPF Checking Only

```csharp
server.AddSpfCheck(failScore: 50);
```

### RBL/DNSBL Checking

```csharp
server.AddRblCheck(
    "zen.spamhaus.org",
    "bl.spamcop.net",
    "b.barracudacentral.org"
);
```

### Greylisting

```csharp
server.AddGreylisting(initialDelay: TimeSpan.FromMinutes(5));

// Whitelist trusted domains
server.WhitelistDomain("trusted.com");
```

### Bayesian Filtering

```csharp
server.AddBayesianFilter(spamThreshold: 0.9);

// Train the filter
await server.TrainBayesianSpamAsync(spamSamples);
await server.TrainBayesianHamAsync(legitimateSamples);
```

## 🔧 Advanced Configuration

### Custom Spam Checker

```csharp
using Zetian.AntiSpam.Abstractions;

public class CustomSpamChecker : ISpamChecker
{
    public string Name => "Custom";
    public bool IsEnabled { get; set; } = true;

    public async Task<SpamCheckResult> CheckAsync(
        ISmtpMessage message,
        ISmtpSession session,
        CancellationToken cancellationToken)
    {
        // Custom logic here
        if (IsSpam(message))
        {
            return SpamCheckResult.Spam(75, "Custom rule triggered");
        }
        
        return SpamCheckResult.Clean(0);
    }
}

// Add custom checker
server.AddSpamChecker(new CustomSpamChecker());
```

### Builder Pattern

```csharp
var builder = new AntiSpamBuilder()
    .WithDnsClient(customDnsClient)
    .EnableSpf()
    .EnableRbl(new[]
    {
        new RblProvider
        {
            Name = "Custom RBL",
            Zone = "custom.rbl.org",
            ExpectedResponses = ["127.0.0.2"]
        }
    })
    .EnableBayesian()
    .AddChecker(new CustomSpamChecker());

var antiSpamService = builder.Build();
```

## 📊 Monitoring & Statistics

```csharp
// Get anti-spam statistics
var stats = server.GetAntiSpamStatistics();
Console.WriteLine($"Messages Checked: {stats.MessagesChecked}");
Console.WriteLine($"Messages Blocked: {stats.MessagesBlocked}");
Console.WriteLine($"Block Rate: {stats.BlockRate:P}");

// Get Bayesian statistics
var service = server.GetAntiSpamService();
var bayesian = service.GetChecker<BayesianSpamFilter>();
var bayesianStats = bayesian.GetStatistics();
Console.WriteLine($"Spam Words: {bayesianStats.TotalSpamMessages}");
Console.WriteLine($"Ham Words: {bayesianStats.TotalHamMessages}");
```

## 🎓 Training Bayesian Filter

### From Files

```csharp
// Load training data
var spamEmails = Directory.GetFiles("spam/", "*.eml")
    .Select(File.ReadAllText);

var hamEmails = Directory.GetFiles("ham/", "*.eml")
    .Select(File.ReadAllText);

// Train the filter
await server.TrainBayesianSpamAsync(spamEmails);
await server.TrainBayesianHamAsync(hamEmails);
```

### Interactive Training

```csharp
server.MessageReceived += async (sender, e) =>
{
    // Let user classify messages
    if (UserMarksAsSpam(e.Message))
    {
        var content = ExtractContent(e.Message);
        await bayesian.TrainSpamAsync(content);
    }
};
```

## 🛠️ Score Interpretation

| Score Range | Action | Description |
|------------|--------|-------------|
| 0-30 | Accept | Clean message |
| 30-50 | Accept with caution | Possible spam indicators |
| 50-70 | Greylist/Temp Reject | Likely spam |
| 70-90 | Reject | High confidence spam |
| 90-100 | Hard Reject | Definite spam |

## 🔍 SPF Results

| Result | Score Impact | Meaning |
|--------|-------------|---------|
| Pass | 0 | Sender authorized |
| None | 5 | No SPF record |
| Neutral | 10 | Neither authorized nor forbidden |
| SoftFail | 30 | Probably not authorized |
| Fail | 50 | Not authorized |

## 🌐 Default RBL Providers

- **SpamCop** - User-reported spam
- **Barracuda** - Commercial spam database
- **Spamhaus ZEN** - Comprehensive spam/exploit list
- **SORBS** - Multiple spam databases (disabled by default)
- **UCEPROTECT** - Aggressive blocking (disabled by default)

## ⚙️ Configuration Options

```csharp
public class AntiSpamOptions
{
    // Score threshold for rejection (default: 50)
    public double RejectThreshold { get; set; }
    
    // Score threshold for temp failure (default: 30)
    public double TempFailThreshold { get; set; }
    
    // Run checks in parallel (default: true)
    public bool RunChecksInParallel { get; set; }
    
    // Timeout per checker (default: 10 seconds)
    public TimeSpan CheckerTimeout { get; set; }
    
    // Continue after spam detection (default: false)
    public bool ContinueOnSpamDetection { get; set; }
    
    // Enable detailed logging (default: false)
    public bool EnableDetailedLogging { get; set; }
}
```

## 📚 Best Practices

1. **Regular Updates** - Keep RBL lists and training data current
2. **Whitelist Partners** - Add trusted partners to greylisting whitelist
3. **Train Bayesian** - Provide adequate training data for better accuracy
4. **Monitor Statistics** - Regularly review block rates and adjust thresholds
5. **Use Multiple Methods** - Combine different checkers for better detection
6. **Start Conservative** - Begin with lenient settings and adjust based on false positives

## 🔒 Security Considerations

- Bayesian filters need balanced training data
- Greylisting may delay legitimate mail initially
- RBL queries may expose client IPs to third parties
- SPF checks require DNS lookups - ensure reliable DNS

## 🚀 Performance Tips

- Limit RBL providers to essential ones
- Set appropriate timeouts for DNS queries
- Pre-train Bayesian filter before deployment
- Use caching DNS client for repeated lookups
- Enable parallel checking for better performance

## 📋 Requirements

- Windows, Linux, or macOS
- .NET 6.0, 7.0, 8.0, 9.0, or 10.0
- Zetian SMTP Server package
- DnsClient.NET for DNS lookups

## 📚 Documentation & Support

- **Issues**: [GitHub Issues](https://github.com/Taiizor/Zetian/issues)
- **Examples**: [GitHub Examples](https://github.com/Taiizor/Zetian/tree/develop/examples)
- **Discussions**: [GitHub Discussions](https://github.com/Taiizor/Zetian/discussions)
- **Documentation**: [Zetian Documentation](https://zetian.soferity.com)

## 📄 License

MIT License - see [LICENSE](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

---

**Built with ❤️ for the .NET community**