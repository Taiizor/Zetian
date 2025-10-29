# Zetian.AntiSpam - Advanced Spam Protection for SMTP

[![NuGet-Version](https://img.shields.io/nuget/v/Zetian.AntiSpam.svg?label=NuGet)](https://www.nuget.org/packages/Zetian.AntiSpam)
[![NuGet-Download](https://img.shields.io/nuget/dt/Zetian.AntiSpam?label=Download)](https://www.nuget.org/api/v2/package/Zetian.AntiSpam)
[![License](https://img.shields.io/github/license/Taiizor/Zetian.svg?label=License)](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

Advanced anti-spam extension for Zetian SMTP Server featuring multiple protection layers including SPF validation, RBL/DNSBL checks, Bayesian filtering, and customizable spam scoring system. Protect your SMTP server from spam, phishing, and email abuse.

## ⚡ Features

- **SPF Validation** - Sender Policy Framework authentication
- **RBL/DNSBL Checks** - Real-time blackhole list verification  
- **Bayesian Filtering** - Machine learning-based spam detection
- **Flexible Scoring** - Customizable weighted scoring system
- **Multiple Actions** - Reject, quarantine, mark, or greylist spam
- **High Performance** - Parallel checking with configurable timeouts
- **Easy Integration** - Fluent API with builder pattern
- **Extensible** - Add custom spam checkers easily

## 📦 Installation

```bash
# Install Zetian SMTP Server (required)
dotnet add package Zetian

# Install AntiSpam Extension  
dotnet add package Zetian.AntiSpam
```

## 🚀 Quick Start

### Basic Usage with Default Settings

```csharp
using Zetian.Server;
using Zetian.AntiSpam.Extensions;

// Create SMTP server with basic anti-spam protection
var server = new SmtpServerBuilder()
    .Port(587)
    .WithBasicAntiSpam()  // Adds SPF, RBL, and Bayesian checks
    .Build();

await server.StartAsync();
```

### Custom Configuration

```csharp
using Zetian.Server;
using Zetian.AntiSpam.Extensions;

var server = new SmtpServerBuilder()
    .Port(587)
    .WithAntiSpam(antiSpam => antiSpam
        .AddSpfChecker(spf => spf.FailAction = SpamAction.Reject)
        .AddRblChecker(rbl => 
        {
            rbl.SpamThreshold = 50;
            rbl.Servers.Add(new RblServer 
            { 
                Name = "Custom RBL",
                Host = "rbl.example.com"
            });
        })
        .AddBayesianChecker(bayes => bayes.SpamThreshold = 0.8)
        .WithSpamThreshold(60)
        .WithRejectThreshold(80)
    )
    .Build();

await server.StartAsync();
```

### Strict Anti-Spam Configuration

```csharp
// Use strict pre-configured settings
var server = new SmtpServerBuilder()
    .Port(587)
    .WithStrictAntiSpam()  // Lower thresholds, stop on first reject
    .Build();
```

## 🛠️ Advanced Configuration

### Training Bayesian Filter

```csharp
// Train the Bayesian filter with known spam/ham
server.TrainBayesianFilter("Buy cheap viagra now!", isSpam: true);
server.TrainBayesianFilter("Meeting scheduled for tomorrow", isSpam: false);

// Batch training
server.TrainBayesianFilterBatch(
    ("Get rich quick scheme", true),
    ("Project status update", false),
    ("Nigerian prince needs help", true),
    ("Quarterly report attached", false)
);
```

### Adding Custom Spam Checkers

```csharp
server.WithAntiSpam(antiSpam => antiSpam
    .AddFunctionChecker("CustomCheck", context =>
    {
        // Custom spam logic
        if (context.FromAddress.EndsWith(".ru"))
        {
            return SpamCheckResult.Spam("CustomCheck", 70, "Suspicious domain");
        }
        return SpamCheckResult.NotSpam("CustomCheck");
    })
    .AddAsyncFunctionChecker("DatabaseCheck", async context =>
    {
        var isBlacklisted = await CheckDatabaseAsync(context.FromAddress);
        return isBlacklisted 
            ? SpamCheckResult.Spam("DatabaseCheck", 100, "Blacklisted sender")
            : SpamCheckResult.NotSpam("DatabaseCheck");
    })
);
```

### Content and Subject Filtering

```csharp
// Add content-based filtering
server.AddContentFilter(
    spamKeywords: new[] { "viagra", "casino", "lottery" },
    scorePerKeyword: 25.0
);

// Add subject line filtering with regex patterns
server.AddSubjectFilter(new[] 
{
    @"^\[SPAM\]",
    @"(winner|congratulations|urgent)",
    @"\${3,}"  // Three or more dollar signs
});
```

### Quarantine Mode

```csharp
// Quarantine spam instead of rejecting
server.UseQuarantineMode(@"C:\smtp_quarantine");

// Messages marked as spam will be saved to quarantine folder
// with headers indicating spam status and score
```

## 📊 Spam Checkers

### SPF (Sender Policy Framework)

Validates sender's IP against domain's SPF records:

```csharp
.AddSpfChecker(config =>
{
    config.Enabled = true;
    config.Weight = 1.0;
    config.FailAction = SpamAction.Reject;
    config.SoftFailAction = SpamAction.Mark;
    config.NoRecordAction = SpamAction.None;
})
```

### RBL/DNSBL (Realtime Blackhole Lists)

Checks IP reputation against multiple blacklists:

```csharp
.AddRblChecker(config =>
{
    config.SpamThreshold = 50.0;
    config.RejectThreshold = 2;  // Reject if listed in 2+ RBLs
    config.SkipForAuthenticatedUsers = true;
    
    // Configure specific RBL servers
    config.Servers = new List<RblServer>
    {
        new() { Name = "Spamhaus", Host = "zen.spamhaus.org", Score = 50 },
        new() { Name = "Barracuda", Host = "b.barracudacentral.org", Score = 40 },
        new() { Name = "SpamCop", Host = "bl.spamcop.net", Score = 35 }
    };
})
```

### Bayesian Filter

Machine learning-based spam detection:

```csharp
.AddBayesianChecker(config =>
{
    config.SpamThreshold = 0.8;  // 80% probability = spam
    config.UseBiGrams = true;     // Use word pairs for better accuracy
    config.MaxTokensToConsider = 15;
    config.MinTokenLength = 3;
    config.HighConfidenceThreshold = 0.95;  // Auto-reject at 95%
})
```

## 🎯 Spam Actions

The anti-spam service can take different actions based on spam scores:

- **None** - Message is clean, no action needed
- **Mark** - Accept but add spam headers
- **Quarantine** - Move to quarantine folder for review  
- **Reject** - Reject with SMTP error code
- **Greylist** - Temporarily defer (legitimate servers retry)
- **Discard** - Silently drop the message

## 📈 Spam Scoring

Each checker contributes to an overall spam score (0-100):

```csharp
// Configure score thresholds
.WithSpamThreshold(50)      // Mark as spam above 50
.WithQuarantineThreshold(70) // Quarantine above 70  
.WithRejectThreshold(85)     // Reject above 85
```

## 🔍 Accessing Results

Anti-spam results are available in message events:

```csharp
server.MessageReceived += (sender, e) =>
{
    if (e.Context.TryGetValue("spam_result", out var result))
    {
        var spamResult = result as AntiSpamResult;
        Console.WriteLine($"Spam Score: {spamResult.TotalScore}");
        Console.WriteLine($"Is Spam: {spamResult.IsSpam}");
        Console.WriteLine($"Action: {spamResult.Action}");
        
        // Individual checker results
        foreach (var check in spamResult.CheckResults)
        {
            Console.WriteLine($"{check.CheckerName}: {check.Score}");
        }
    }
};
```

## 📋 Message Headers

The following headers are added to processed messages:

- `X-Spam-Check-By: Zetian.AntiSpam`
- `X-Spam-Score: 75.5`
- `X-Spam-Status: Yes/No`
- `X-Spam-Checker-Scores: SPF=30.0, RBL=45.5, Bayesian=0.0`
- `X-Spam-Reasons: Listed in Spamhaus ZEN; SPF check failed`
- `X-Spam-Quarantine: /path/to/quarantine/file.eml` (if quarantined)

## 🏗️ Builder Pattern

Create custom anti-spam configurations using the builder:

```csharp
using Zetian.AntiSpam.Builders;

var antiSpamService = AntiSpamBuilder.Create()
    .WithLoggerFactory(loggerFactory)
    .WithDnsClient(customDnsClient)
    .AddSpfChecker()
    .AddRblChecker()
    .AddBayesianChecker()
    .AddCustomChecker(myCustomChecker)
    .WithSpamThreshold(50)
    .WithRejectThreshold(80)
    .RunInParallel(true)
    .WithCheckerTimeout(TimeSpan.FromSeconds(5))
    .Build();
```

## 🔒 Best Practices

1. **Start with default settings** and adjust based on false positives/negatives
2. **Train the Bayesian filter** with your actual spam/ham for best results
3. **Use multiple checkers** for better accuracy
4. **Configure RBL servers carefully** - some are more aggressive than others
5. **Monitor logs** to fine-tune scoring thresholds
6. **Consider greylisting** for unknown senders
7. **Whitelist authenticated users** from certain checks
8. **Use quarantine mode** initially to evaluate effectiveness

## 📊 Performance Considerations

- Checks run in parallel by default for better performance
- DNS lookups are cached automatically
- Configure timeouts to prevent slow checks from blocking
- Consider disabling expensive checks for authenticated users
- Use `StopOnFirstReject` for faster rejection of obvious spam

## 📚 Examples

See the [examples](https://github.com/Taiizor/Zetian/tree/develop/examples/Zetian.AntiSpam.Examples) folder for more detailed usage examples.

## 🆘 Support

- **Issues**: [GitHub Issues](https://github.com/Taiizor/Zetian/issues)
- **Discussions**: [GitHub Discussions](https://github.com/Taiizor/Zetian/discussions)
- **Documentation**: [Zetian Documentation](https://zetian.soferity.com)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.