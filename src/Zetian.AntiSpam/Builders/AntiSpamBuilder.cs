using DnsClient;
using System;
using System.Collections.Generic;
using Zetian.AntiSpam.Abstractions;
using Zetian.AntiSpam.Checkers;
using Zetian.AntiSpam.Services;

namespace Zetian.AntiSpam.Builders
{
    /// <summary>
    /// Builder for configuring anti-spam features
    /// </summary>
    public class AntiSpamBuilder
    {
        private readonly List<ISpamChecker> _checkers = [];
        private ILookupClient? _dnsClient;
        private AntiSpamOptions _options = new();

        /// <summary>
        /// Use default anti-spam configuration
        /// </summary>
        public AntiSpamBuilder UseDefaults()
        {
            EnableSpf();
            EnableRbl();
            EnableBayesian();
            return this;
        }

        /// <summary>
        /// Use aggressive anti-spam configuration
        /// </summary>
        public AntiSpamBuilder UseAggressive()
        {
            EnableSpf(failScore: 70);
            EnableRbl(scorePerListing: 35);
            EnableBayesian(spamThreshold: 0.7);
            EnableGreylisting(initialDelay: TimeSpan.FromMinutes(10));
            return this;
        }

        /// <summary>
        /// Use lenient anti-spam configuration
        /// </summary>
        public AntiSpamBuilder UseLenient()
        {
            EnableSpf(failScore: 30, softFailScore: 15);
            EnableRbl(scorePerListing: 15);
            EnableBayesian(spamThreshold: 0.95);
            return this;
        }

        /// <summary>
        /// Set custom DNS client for lookups
        /// </summary>
        public AntiSpamBuilder WithDnsClient(ILookupClient dnsClient)
        {
            _dnsClient = dnsClient;
            return this;
        }

        /// <summary>
        /// Configure general options
        /// </summary>
        public AntiSpamBuilder WithOptions(Action<AntiSpamOptions> configure)
        {
            configure(_options);
            return this;
        }

        /// <summary>
        /// Enable SPF checking
        /// </summary>
        public AntiSpamBuilder EnableSpf(
            double failScore = 50,
            double softFailScore = 30,
            double neutralScore = 10,
            double noneScore = 5)
        {
            _checkers.Add(new SpfChecker(
                _dnsClient,
                failScore,
                softFailScore,
                neutralScore,
                noneScore));
            return this;
        }

        /// <summary>
        /// Enable RBL/DNSBL checking
        /// </summary>
        public AntiSpamBuilder EnableRbl(
            IEnumerable<RblProvider>? providers = null,
            double scorePerListing = 25,
            double maxScore = 100)
        {
            _checkers.Add(new RblChecker(
                _dnsClient,
                providers,
                scorePerListing,
                maxScore));
            return this;
        }

        /// <summary>
        /// Enable RBL with specific zones
        /// </summary>
        public AntiSpamBuilder EnableRbl(params string[] zones)
        {
            var providers = new List<RblProvider>();
            foreach (string zone in zones)
            {
                providers.Add(new RblProvider
                {
                    Name = zone,
                    Zone = zone,
                    IsEnabled = true
                });
            }
            return EnableRbl(providers);
        }

        /// <summary>
        /// Enable Bayesian spam filtering
        /// </summary>
        public AntiSpamBuilder EnableBayesian(
            double spamThreshold = 0.9,
            double unknownWordProbability = 0.5)
        {
            _checkers.Add(new BayesianSpamFilter(
                spamThreshold,
                unknownWordProbability));
            return this;
        }

        /// <summary>
        /// Enable greylisting
        /// </summary>
        public AntiSpamBuilder EnableGreylisting(
            TimeSpan? initialDelay = null,
            TimeSpan? whitelistDuration = null,
            TimeSpan? maxRetryTime = null,
            bool autoWhitelist = true)
        {
            _checkers.Add(new GreylistingChecker(
                initialDelay,
                whitelistDuration,
                maxRetryTime,
                autoWhitelist));
            return this;
        }

        /// <summary>
        /// Add a custom spam checker
        /// </summary>
        public AntiSpamBuilder AddChecker(ISpamChecker checker)
        {
            _checkers.Add(checker);
            return this;
        }

        /// <summary>
        /// Build the anti-spam service
        /// </summary>
        public AntiSpamService Build()
        {
            return new AntiSpamService(_checkers, _options);
        }
    }

    /// <summary>
    /// Options for anti-spam configuration
    /// </summary>
    public class AntiSpamOptions
    {
        /// <summary>
        /// Gets or sets the spam score threshold for rejection
        /// </summary>
        public double RejectThreshold { get; set; } = 50;

        /// <summary>
        /// Gets or sets the spam score threshold for temporary failure
        /// </summary>
        public double TempFailThreshold { get; set; } = 30;

        /// <summary>
        /// Gets or sets whether to run checks in parallel
        /// </summary>
        public bool RunChecksInParallel { get; set; } = true;

        /// <summary>
        /// Gets or sets the timeout for each checker
        /// </summary>
        public TimeSpan CheckerTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets or sets whether to continue checking after first spam detection
        /// </summary>
        public bool ContinueOnSpamDetection { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to log detailed check results
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;

        /// <summary>
        /// Gets or sets custom headers to add to messages
        /// </summary>
        public Dictionary<string, string> CustomHeaders { get; set; } = new()
        {
            ["X-Spam-Checker"] = "Zetian.AntiSpam"
        };
    }
}
