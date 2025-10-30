'use client';

import { 
  Shield, Filter, Brain, Globe, Mail, CheckCircle,
  Settings, BarChart, Zap, Clock, TrendingUp
} from 'lucide-react';
import CodeBlock from '@/components/CodeBlock';
import Link from 'next/link';

const spfExample = `// Add SPF checking
server.AddSpfCheck(failScore: 50);`;

const dkimExample = `// Add DKIM checking with strict mode
server.AddDkimCheck(
    failScore: 40,
    strictMode: true  // Require all essential headers to be signed
);`;

const dmarcExample = `// Add DMARC checking with policy enforcement
server.AddDmarcCheck(
    failScore: 70,        // Score for reject policy
    quarantineScore: 50,  // Score for quarantine policy
    enforcePolicy: true   // Enforce DMARC policies
);`;

const authExample = `// Add all three authentication methods in one call
server.AddEmailAuthentication(
    strictMode: true,      // Strict DKIM validation
    enforcePolicy: true    // Enforce DMARC policies
);`;

const rblExample = `// Add RBL checking with popular lists
server.AddRblCheck(
    "zen.spamhaus.org",       // Spamhaus ZEN
    "bl.spamcop.net",         // SpamCop
    "b.barracudacentral.org"  // Barracuda
);`;

const bayesianExample = `// Option 1: Add Bayesian to server
server.AddBayesianFilter(spamThreshold: 0.9);

// Option 2: Work with Bayesian directly for training
var bayesianFilter = new BayesianSpamFilter(spamThreshold: 0.9);
await bayesianFilter.TrainSpamAsync("spam content");
await bayesianFilter.TrainHamAsync("legitimate content");

// Then add trained filter to server
server.AddSpamChecker(bayesianFilter);`;

const trainExample = `// Create and train Bayesian filter
var bayesianFilter = new BayesianSpamFilter();

// Load training data from files
var spamEmails = Directory.GetFiles("spam/", "*.eml")
    .Select(File.ReadAllText);

var hamEmails = Directory.GetFiles("ham/", "*.eml")
    .Select(File.ReadAllText);

// Train the filter
foreach (var spam in spamEmails)
{
    await bayesianFilter.TrainSpamAsync(spam);
}

foreach (var ham in hamEmails)
{
    await bayesianFilter.TrainHamAsync(ham);
}

// Add trained filter to server
server.AddSpamChecker(bayesianFilter);`;

const greylistExample = `// Add greylisting
server.AddGreylisting(initialDelay: TimeSpan.FromMinutes(5));

// With domain whitelisting
var greylistingChecker = new GreylistingChecker(
    initialDelay: TimeSpan.FromMinutes(5));
greylistingChecker.Whitelist("trusted.com");
server.AddSpamChecker(greylistingChecker);`;

const customExample = `using Zetian.AntiSpam.Abstractions;

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
    
    private bool IsSpam(ISmtpMessage message)
    {
        // Check for suspicious patterns
        var subject = message.Subject?.ToLower() ?? "";
        
        // Example: Check for common spam keywords
        var spamKeywords = new[] { "viagra", "lottery", "winner", "prize" };
        if (spamKeywords.Any(keyword => subject.Contains(keyword)))
            return true;
            
        // Example: Check for excessive caps
        if (subject.Count(char.IsUpper) > subject.Length * 0.8)
            return true;
            
        return false;
    }
}

// Add custom checker
server.AddSpamChecker(new CustomSpamChecker());`;

export default function AntiSpamPage() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-slate-800 to-gray-900">
      <div className="relative">
        <div className="absolute inset-0">
          <div className="absolute inset-0 bg-grid-white/[0.02]" />
        </div>
        
        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          {/* Header */}
          <div className="text-center mb-12">
            <div className="inline-flex items-center justify-center w-20 h-20 bg-gradient-to-br from-red-500 to-orange-600 rounded-2xl mb-6">
              <Shield className="w-10 h-10 text-white" />
            </div>
            <h1 className="text-5xl font-bold text-white mb-4">Zetian.AntiSpam</h1>
            <p className="text-xl text-gray-300 mb-6">
              Advanced Spam Protection for Zetian SMTP Server
            </p>
            <div className="flex justify-center gap-4">
              <a href="https://www.nuget.org/packages/Zetian.AntiSpam" className="inline-flex items-center px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700">
                <span className="mr-2">📦</span> NuGet Package
              </a>
              <a href="https://github.com/Taiizor/Zetian" className="inline-flex items-center px-4 py-2 bg-gray-700 text-white rounded-lg hover:bg-gray-600">
                <span className="mr-2">⭐</span> Star on GitHub
              </a>
            </div>
          </div>

          {/* Features */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-12">
            {[
              { icon: Brain, title: 'Bayesian Filtering', desc: 'Machine learning-based spam detection' },
              { icon: Shield, title: 'SPF/DKIM/DMARC', desc: 'Complete email authentication suite' },
              { icon: Globe, title: 'RBL/DNSBL', desc: 'Realtime blackhole list checking' },
              { icon: Clock, title: 'Greylisting', desc: 'Temporary rejection for unknown senders' },
              { icon: Filter, title: 'Custom Filters', desc: 'Extensible with custom spam checkers' },
              { icon: BarChart, title: 'Statistics', desc: 'Detailed metrics and reporting' },
            ].map((feature, i) => (
              <div key={i} className="bg-gray-800/50 backdrop-blur-sm rounded-xl p-6 border border-gray-700/50">
                <feature.icon className="w-10 h-10 text-red-400 mb-3" />
                <h3 className="text-lg font-semibold text-white mb-2">{feature.title}</h3>
                <p className="text-gray-400 text-sm">{feature.desc}</p>
              </div>
            ))}
          </div>

          {/* Quick Start */}
          <div className="bg-gray-800/30 backdrop-blur-sm rounded-2xl p-8 mb-12">
            <h2 className="text-3xl font-bold text-white mb-6 flex items-center">
              <Zap className="w-8 h-8 mr-3 text-yellow-400" />
              Quick Start
            </h2>

            <div className="space-y-6">
              <div>
                <h3 className="text-xl font-semibold text-white mb-3">Basic Anti-Spam Setup</h3>
                <CodeBlock language="csharp" code={`using Zetian.Server;
using Zetian.AntiSpam.Extensions;

// Create SMTP server
var server = new SmtpServerBuilder()
    .Port(25)
    .ServerName("My SMTP Server")
    .Build();

// Add anti-spam with default settings
server.AddAntiSpam();

await server.StartAsync();`} />
              </div>

              <div>
                <h3 className="text-xl font-semibold text-white mb-3">Custom Configuration</h3>
                <CodeBlock language="csharp" code={`// Configure anti-spam features
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
    }));`} />
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="bg-gray-900/50 rounded-lg p-4">
                  <h4 className="text-lg font-semibold text-green-400 mb-2">Lenient Protection</h4>
                  <CodeBlock language="csharp" code={`// For trusted environments
server.AddAntiSpam(builder => 
    builder.UseLenient());`} />
                </div>
                <div className="bg-gray-900/50 rounded-lg p-4">
                  <h4 className="text-lg font-semibold text-red-400 mb-2">Aggressive Protection</h4>
                  <CodeBlock language="csharp" code={`// Maximum spam protection
server.AddAntiSpam(builder => 
    builder.UseAggressive());`} />
                </div>
              </div>
            </div>
          </div>

          {/* Email Authentication */}
          <div className="bg-gray-800/30 backdrop-blur-sm rounded-2xl p-8 mb-12">
            <h2 className="text-3xl font-bold text-white mb-6 flex items-center">
              <Mail className="w-8 h-8 mr-3 text-blue-400" />
              Email Authentication (SPF, DKIM, DMARC)
            </h2>

            <div className="space-y-6">
              <div>
                <h3 className="text-xl font-semibold text-white mb-3">SPF (Sender Policy Framework)</h3>
                <p className="text-gray-300 mb-3">Verifies that the sending server is authorized to send email for the domain:</p>
                <CodeBlock language="csharp" code={spfExample} />
                <div className="mt-4 bg-gray-900/50 rounded-lg p-4">
                  <h4 className="text-sm font-semibold text-gray-300 mb-2">SPF Results & Scores</h4>
                  <div className="space-y-1 text-sm">
                    <div className="flex justify-between"><span className="text-green-400">Pass</span><span className="text-gray-400">0 points</span></div>
                    <div className="flex justify-between"><span className="text-gray-400">None</span><span className="text-gray-400">5 points</span></div>
                    <div className="flex justify-between"><span className="text-yellow-400">Neutral</span><span className="text-gray-400">10 points</span></div>
                    <div className="flex justify-between"><span className="text-orange-400">SoftFail</span><span className="text-gray-400">30 points</span></div>
                    <div className="flex justify-between"><span className="text-red-400">Fail</span><span className="text-gray-400">50 points</span></div>
                  </div>
                </div>
              </div>

              <div>
                <h3 className="text-xl font-semibold text-white mb-3">DKIM (DomainKeys Identified Mail)</h3>
                <p className="text-gray-300 mb-3">Verifies email authenticity using digital signatures:</p>
                <CodeBlock language="csharp" code={dkimExample} />
              </div>

              <div>
                <h3 className="text-xl font-semibold text-white mb-3">DMARC (Domain-based Message Authentication)</h3>
                <p className="text-gray-300 mb-3">Enforces policies based on SPF and DKIM results:</p>
                <CodeBlock language="csharp" code={dmarcExample} />
              </div>

              <div>
                <h3 className="text-xl font-semibold text-white mb-3">Complete Email Authentication</h3>
                <CodeBlock language="csharp" code={authExample} />
              </div>
            </div>
          </div>

          {/* RBL/DNSBL */}
          <div className="bg-gray-800/30 backdrop-blur-sm rounded-2xl p-8 mb-12">
            <h2 className="text-3xl font-bold text-white mb-6 flex items-center">
              <Globe className="w-8 h-8 mr-3 text-purple-400" />
              RBL/DNSBL Checking
            </h2>
            <CodeBlock language="csharp" code={rblExample} />
          </div>

          {/* Bayesian */}
          <div className="bg-gray-800/30 backdrop-blur-sm rounded-2xl p-8 mb-12">
            <h2 className="text-3xl font-bold text-white mb-6 flex items-center">
              <Brain className="w-8 h-8 mr-3 text-green-400" />
              Bayesian Filtering
            </h2>
            <div className="space-y-6">
              <div>
                <h3 className="text-xl font-semibold text-white mb-3">Machine Learning Spam Detection</h3>
                <CodeBlock language="csharp" code={bayesianExample} />
              </div>
              <div>
                <h3 className="text-xl font-semibold text-white mb-3">Training the Filter</h3>
                <CodeBlock language="csharp" code={trainExample} />
              </div>
            </div>
          </div>

          {/* Greylisting */}
          <div className="bg-gray-800/30 backdrop-blur-sm rounded-2xl p-8 mb-12">
            <h2 className="text-3xl font-bold text-white mb-6 flex items-center">
              <Clock className="w-8 h-8 mr-3 text-orange-400" />
              Greylisting
            </h2>
            <CodeBlock language="csharp" code={greylistExample} />
            <div className="mt-4 bg-gray-900/50 rounded-lg p-4">
              <h4 className="text-lg font-semibold text-yellow-400 mb-2">How It Works</h4>
              <ol className="space-y-2 text-gray-300">
                <li>1. First email attempt is temporarily rejected</li>
                <li>2. Legitimate servers retry after a delay</li>
                <li>3. Retry is accepted and sender is whitelisted</li>
                <li>4. Spam bots rarely retry, filtering them out</li>
              </ol>
            </div>
          </div>

          {/* Custom Checker */}
          <div className="bg-gray-800/30 backdrop-blur-sm rounded-2xl p-8 mb-12">
            <h2 className="text-3xl font-bold text-white mb-6 flex items-center">
              <Settings className="w-8 h-8 mr-3 text-cyan-400" />
              Custom Spam Checker
            </h2>
            <CodeBlock language="csharp" code={customExample} />
          </div>

          {/* Score Interpretation */}
          <div className="bg-gray-800/30 backdrop-blur-sm rounded-2xl p-8 mb-12">
            <h2 className="text-3xl font-bold text-white mb-6 flex items-center">
              <TrendingUp className="w-8 h-8 mr-3 text-pink-400" />
              Score Interpretation
            </h2>
            <div className="bg-gray-900/50 rounded-lg overflow-hidden">
              <table className="w-full">
                <thead className="bg-gray-800/50">
                  <tr>
                    <th className="px-4 py-3 text-left text-gray-300">Score</th>
                    <th className="px-4 py-3 text-left text-gray-300">Action</th>
                    <th className="px-4 py-3 text-left text-gray-300">Description</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-700/50">
                  <tr><td className="px-4 py-3 text-green-400">0-30</td><td className="px-4 py-3">Accept</td><td className="px-4 py-3 text-gray-400">Clean message</td></tr>
                  <tr><td className="px-4 py-3 text-yellow-400">30-50</td><td className="px-4 py-3">Accept with caution</td><td className="px-4 py-3 text-gray-400">Possible spam</td></tr>
                  <tr><td className="px-4 py-3 text-orange-400">50-70</td><td className="px-4 py-3">Greylist/Temp Reject</td><td className="px-4 py-3 text-gray-400">Likely spam</td></tr>
                  <tr><td className="px-4 py-3 text-red-400">70-90</td><td className="px-4 py-3">Reject</td><td className="px-4 py-3 text-gray-400">High confidence spam</td></tr>
                  <tr><td className="px-4 py-3 text-red-600">90-100</td><td className="px-4 py-3">Hard Reject</td><td className="px-4 py-3 text-gray-400">Definite spam</td></tr>
                </tbody>
              </table>
            </div>
          </div>

          {/* Best Practices */}
          <div className="bg-gradient-to-br from-red-900/20 to-orange-900/20 rounded-2xl p-8 mb-12">
            <h2 className="text-3xl font-bold text-white mb-6">Best Practices</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <ul className="space-y-2 text-gray-300">
                <li className="flex items-start">
                  <CheckCircle className="w-5 h-5 text-green-400 mr-2 mt-0.5" />
                  <span>Start with lenient settings and adjust</span>
                </li>
                <li className="flex items-start">
                  <CheckCircle className="w-5 h-5 text-green-400 mr-2 mt-0.5" />
                  <span>Train Bayesian filters with balanced data</span>
                </li>
                <li className="flex items-start">
                  <CheckCircle className="w-5 h-5 text-green-400 mr-2 mt-0.5" />
                  <span>Whitelist trusted partners</span>
                </li>
              </ul>
              <ul className="space-y-2 text-gray-300">
                <li className="flex items-start">
                  <CheckCircle className="w-5 h-5 text-green-400 mr-2 mt-0.5" />
                  <span>Monitor block rates regularly</span>
                </li>
                <li className="flex items-start">
                  <CheckCircle className="w-5 h-5 text-green-400 mr-2 mt-0.5" />
                  <span>Combine multiple detection methods</span>
                </li>
                <li className="flex items-start">
                  <CheckCircle className="w-5 h-5 text-green-400 mr-2 mt-0.5" />
                  <span>Keep RBL lists updated</span>
                </li>
              </ul>
            </div>
          </div>

          {/* Navigation */}
          <div className="flex justify-between items-center">
            <Link href="/docs/relay" className="text-blue-400 hover:text-blue-300 flex items-center">
              ← Relay Extension
            </Link>
            <Link href="/docs/extensions" className="text-blue-400 hover:text-blue-300 flex items-center">
              Back to Extensions →
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}