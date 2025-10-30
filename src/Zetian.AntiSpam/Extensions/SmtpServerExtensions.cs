using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.AntiSpam.Abstractions;
using Zetian.AntiSpam.Builders;
using Zetian.AntiSpam.Checkers;
using Zetian.AntiSpam.Models;
using Zetian.AntiSpam.Services;
using Zetian.Protocol;

namespace Zetian.AntiSpam.Extensions
{
    /// <summary>
    /// Extension methods for integrating anti-spam features with SMTP server
    /// </summary>
    public static class SmtpServerExtensions
    {
        private const string AntiSpamServiceKey = "Zetian.AntiSpam.Service";

        /// <summary>
        /// Adds comprehensive anti-spam protection with default settings
        /// </summary>
        public static ISmtpServer AddAntiSpam(this ISmtpServer server)
        {
            return server.AddAntiSpam(builder => builder.UseDefaults());
        }

        /// <summary>
        /// Adds anti-spam protection with custom configuration
        /// </summary>
        public static ISmtpServer AddAntiSpam(
            this ISmtpServer server,
            Action<AntiSpamBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(server);
            ArgumentNullException.ThrowIfNull(configure);

            AntiSpamBuilder builder = new();
            configure(builder);

            AntiSpamService antiSpamService = builder.Build();

            // Store antispam service in server properties
            server.Configuration.Properties[AntiSpamServiceKey] = antiSpamService;

            // Hook into message received event
            server.MessageReceived += async (sender, e) =>
            {
                try
                {
                    SpamCheckResult result = await antiSpamService.CheckMessageAsync(e.Message, e.Session);

                    if (result.IsSpam)
                    {
                        e.Cancel = true;

                        // Determine response code based on severity
                        if (result.Score >= 90)
                        {
                            e.Response = new SmtpResponse(550, $"Message rejected: {result.Reason}");
                        }
                        else if (result.Score >= 70)
                        {
                            e.Response = new SmtpResponse(451, $"Message temporarily rejected: {result.Reason}");
                        }
                        else
                        {
                            e.Response = new SmtpResponse(450, $"Message greylisted: {result.Reason}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't reject message on anti-spam failure
                    Console.WriteLine($"Anti-spam check failed: {ex.Message}");
                }
            };

            return server;
        }

        /// <summary>
        /// Adds SPF checking to the server
        /// </summary>
        public static ISmtpServer AddSpfCheck(this ISmtpServer server, double failScore = 50)
        {
            SpfChecker spfChecker = new(failScore: failScore);
            return server.AddSpamChecker(spfChecker);
        }

        /// <summary>
        /// Adds RBL/DNSBL checking to the server
        /// </summary>
        public static ISmtpServer AddRblCheck(
            this ISmtpServer server,
            params string[] rblZones)
        {
            IEnumerable<RblProvider> providers = rblZones.Select(zone => new RblProvider
            {
                Name = zone,
                Zone = zone,
                IsEnabled = true
            });

            RblChecker rblChecker = new(providers: providers);
            return server.AddSpamChecker(rblChecker);
        }

        /// <summary>
        /// Adds greylisting to the server
        /// </summary>
        public static ISmtpServer AddGreylisting(
            this ISmtpServer server,
            TimeSpan? initialDelay = null)
        {
            GreylistingChecker greylistChecker = new(initialDelay: initialDelay);
            return server.AddSpamChecker(greylistChecker);
        }

        /// <summary>
        /// Adds Bayesian spam filtering to the server
        /// </summary>
        public static ISmtpServer AddBayesianFilter(
            this ISmtpServer server,
            double spamThreshold = 0.9)
        {
            BayesianSpamFilter bayesianFilter = new(spamThreshold: spamThreshold);
            return server.AddSpamChecker(bayesianFilter);
        }

        /// <summary>
        /// Adds DKIM signature checking to the server
        /// </summary>
        public static ISmtpServer AddDkimCheck(
            this ISmtpServer server,
            double failScore = 40,
            bool strictMode = false)
        {
            DkimChecker dkimChecker = new(
                failScore: failScore,
                strictMode: strictMode);
            return server.AddSpamChecker(dkimChecker);
        }

        /// <summary>
        /// Adds DMARC policy checking to the server
        /// </summary>
        public static ISmtpServer AddDmarcCheck(
            this ISmtpServer server,
            double failScore = 70,
            double quarantineScore = 50,
            bool enforcePolicy = true)
        {
            DmarcChecker dmarcChecker = new(
                failScore: failScore,
                quarantineScore: quarantineScore,
                enforcePolicy: enforcePolicy);
            return server.AddSpamChecker(dmarcChecker);
        }

        /// <summary>
        /// Adds full email authentication (SPF + DKIM + DMARC)
        /// </summary>
        public static ISmtpServer AddEmailAuthentication(
            this ISmtpServer server,
            bool strictMode = false,
            bool enforcePolicy = true)
        {
            // Add all authentication methods
            server.AddSpfCheck();
            server.AddDkimCheck(strictMode: strictMode);
            server.AddDmarcCheck(enforcePolicy: enforcePolicy);
            return server;
        }

        /// <summary>
        /// Adds a custom spam checker to the server
        /// </summary>
        public static ISmtpServer AddSpamChecker(
            this ISmtpServer server,
            ISpamChecker checker)
        {
            ArgumentNullException.ThrowIfNull(server);
            ArgumentNullException.ThrowIfNull(checker);

            server.MessageReceived += async (sender, e) =>
            {
                try
                {
                    SpamCheckResult result = await checker.CheckAsync(e.Message, e.Session);

                    if (result.IsSpam && result.Score >= 50)
                    {
                        e.Cancel = true;
                        e.Response = new SmtpResponse(550, $"Message rejected by {checker.Name}: {result.Reason}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{checker.Name} check failed: {ex.Message}");
                }
            };

            return server;
        }

        /// <summary>
        /// Trains the Bayesian filter with spam samples
        /// </summary>
        public static async Task<ISmtpServer> TrainBayesianSpamAsync(
            this ISmtpServer server,
            IEnumerable<string> spamSamples)
        {
            AntiSpamService? service = GetAntiSpamService(server);
            if (service != null)
            {
                BayesianSpamFilter? bayesian = service.GetChecker<BayesianSpamFilter>();
                if (bayesian != null)
                {
                    foreach (string sample in spamSamples)
                    {
                        await bayesian.TrainSpamAsync(sample);
                    }
                }
            }
            return server;
        }

        /// <summary>
        /// Trains the Bayesian filter with legitimate (ham) samples
        /// </summary>
        public static async Task<ISmtpServer> TrainBayesianHamAsync(
            this ISmtpServer server,
            IEnumerable<string> hamSamples)
        {
            AntiSpamService? service = GetAntiSpamService(server);
            if (service != null)
            {
                BayesianSpamFilter? bayesian = service.GetChecker<BayesianSpamFilter>();
                if (bayesian != null)
                {
                    foreach (string sample in hamSamples)
                    {
                        await bayesian.TrainHamAsync(sample);
                    }
                }
            }
            return server;
        }

        /// <summary>
        /// Whitelists a domain for greylisting
        /// </summary>
        public static ISmtpServer WhitelistDomain(
            this ISmtpServer server,
            string domain)
        {
            AntiSpamService? service = GetAntiSpamService(server);
            if (service != null)
            {
                GreylistingChecker? greylisting = service.GetChecker<GreylistingChecker>();
                greylisting?.Whitelist(domain);
            }
            return server;
        }

        /// <summary>
        /// Gets anti-spam statistics
        /// </summary>
        public static AntiSpamStatistics GetAntiSpamStatistics(this ISmtpServer server)
        {
            AntiSpamService? service = GetAntiSpamService(server);
            if (service == null)
            {
                return new AntiSpamStatistics();
            }

            AntiSpamServiceStatistics stats = service.GetStatistics();
            return new AntiSpamStatistics
            {
                RblHits = 0,
                GreylistDelays = 0,
                BayesianBlocks = 0,
                MessagesBlocked = stats.MessagesBlocked,
                MessagesChecked = stats.MessagesChecked,
                SpfFailures = 0, // These would need to be tracked separately
                AverageSpamScore = stats.MessagesChecked > 0 ? stats.BlockRate : 0
            };
        }

        /// <summary>
        /// Gets the antispam service from the server
        /// </summary>
        private static AntiSpamService? GetAntiSpamService(ISmtpServer server)
        {
            if (server?.Configuration?.Properties?.TryGetValue(AntiSpamServiceKey, out object? service) == true)
            {
                return service as AntiSpamService;
            }

            return null;
        }
    }

    /// <summary>
    /// Anti-spam statistics
    /// </summary>
    public class AntiSpamStatistics
    {
        public long MessagesChecked { get; set; }
        public long MessagesBlocked { get; set; }
        public long SpfFailures { get; set; }
        public long RblHits { get; set; }
        public long GreylistDelays { get; set; }
        public long BayesianBlocks { get; set; }
        public double AverageSpamScore { get; set; }

        /// <summary>
        /// Gets the percentage of messages that were blocked
        /// </summary>
        public double BlockRate => MessagesChecked > 0
            ? (double)MessagesBlocked / MessagesChecked * 100
            : 0;
    }
}