using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.AntiSpam.Abstractions;
using Zetian.AntiSpam.Builders;
using Zetian.AntiSpam.Checkers;
using Zetian.AntiSpam.Models;
using Zetian.AntiSpam.Services;
using Zetian.Models.EventArgs;
using Zetian.Protocol;
using Zetian.Server;

namespace Zetian.AntiSpam.Extensions
{
    /// <summary>
    /// Extension methods for integrating AntiSpam with SmtpServer
    /// </summary>
    public static class SmtpServerExtensions
    {
        private static readonly Dictionary<SmtpServer, AntiSpamService> _antiSpamServices = new();
        private static readonly Dictionary<SmtpServer, BayesianChecker> _bayesianCheckers = new();
        private static readonly Dictionary<object, AntiSpamResult> _messageResults = new();

        /// <summary>
        /// Gets the spam check result for a message
        /// </summary>
        public static AntiSpamResult? GetSpamResult(this ISmtpMessage message)
        {
            return _messageResults.TryGetValue(message, out var result) ? result : null;
        }

        /// <summary>
        /// Enables anti-spam protection on the SMTP server
        /// </summary>
        public static SmtpServer EnableAntiSpam(
            this SmtpServer server,
            Action<AntiSpamBuilder>? configureAction = null)
        {
            AntiSpamBuilder antiSpamBuilder = AntiSpamBuilder.Create();
            configureAction?.Invoke(antiSpamBuilder);

            AntiSpamService antiSpamService = antiSpamBuilder.Build();

            // Store the service for later retrieval
            _antiSpamServices[server] = antiSpamService;

            // Store Bayesian checker if available
            BayesianChecker? bayesianChecker = antiSpamService.GetCheckers()
                .OfType<BayesianChecker>()
                .FirstOrDefault();
            if (bayesianChecker != null)
            {
                _bayesianCheckers[server] = bayesianChecker;
            }

            // Hook into the message received event
            server.MessageReceived += async (sender, e) =>
            {
                await ProcessMessageWithAntiSpam(server, e, antiSpamService);
            };

            return server;
        }

        /// <summary>
        /// Enables anti-spam protection with default settings
        /// </summary>
        public static SmtpServer EnableBasicAntiSpam(this SmtpServer server)
        {
            return server.EnableAntiSpam(builder => builder.AddDefaultCheckers());
        }

        /// <summary>
        /// Gets the configured AntiSpam service from the server
        /// </summary>
        public static AntiSpamService? GetAntiSpamService(this SmtpServer server)
        {
            return _antiSpamServices.TryGetValue(server, out AntiSpamService? service) ? service : null;
        }

        /// <summary>
        /// Trains the Bayesian filter with a message
        /// </summary>
        public static void TrainBayesianFilter(
            this SmtpServer server,
            string content,
            bool isSpam)
        {
            if (_bayesianCheckers.TryGetValue(server, out BayesianChecker? checker))
            {
                checker.Train(content, isSpam);
            }
        }

        /// <summary>
        /// Trains the Bayesian filter with multiple messages
        /// </summary>
        public static void TrainBayesianFilterBatch(
            this SmtpServer server,
            params (string content, bool isSpam)[] samples)
        {
            if (_bayesianCheckers.TryGetValue(server, out BayesianChecker? checker))
            {
                checker.TrainBatch(samples);
            }
        }

        /// <summary>
        /// Adds a custom spam checker to the server's anti-spam service
        /// </summary>
        public static SmtpServer AddSpamChecker(
            this SmtpServer server,
            Abstractions.ISpamChecker checker)
        {
            AntiSpamService? antiSpamService = server.GetAntiSpamService();
            antiSpamService?.AddChecker(checker);
            return server;
        }

        /// <summary>
        /// Adds a simple content-based spam filter
        /// </summary>
        public static SmtpServer AddContentFilter(
            this SmtpServer server,
            string[] spamKeywords,
            double scorePerKeyword = 20.0)
        {
            return server.AddSpamChecker(new ContentSpamChecker(spamKeywords, scorePerKeyword));
        }

        /// <summary>
        /// Adds a subject line spam filter
        /// </summary>
        public static SmtpServer AddSubjectFilter(
            this SmtpServer server,
            string[] spamSubjectPatterns)
        {
            return server.AddSpamChecker(new SubjectSpamChecker(spamSubjectPatterns));
        }

        /// <summary>
        /// Configures anti-spam to quarantine messages instead of rejecting
        /// </summary>
        public static SmtpServer UseQuarantineMode(
            this SmtpServer server,
            string quarantinePath)
        {
            server.MessageReceived += async (sender, e) =>
            {
                // Check if we have a spam result for this message
                if (_messageResults.TryGetValue(e.Message, out AntiSpamResult? result))
                {
                    if (result.IsSpam && result.Action == SpamAction.Reject)
                    {
                        // Change action to quarantine instead of reject
                        result.Action = SpamAction.Quarantine;

                        // Save to quarantine folder
                        string fileName = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}_{e.Message.Id}.eml";
                        string fullPath = System.IO.Path.Combine(quarantinePath, fileName);

                        await e.Message.SaveToFileAsync(fullPath);

                        // Don't reject, but mark as spam
                        e.Cancel = false;
                        e.Message.Headers.Add("X-Spam-Status", $"Yes, score={result.TotalScore:F1}");
                        e.Message.Headers.Add("X-Spam-Quarantine", fullPath);
                    }
                }
            };

            return server;
        }

        private static async Task ProcessMessageWithAntiSpam(
            SmtpServer server,
            MessageEventArgs e,
            AntiSpamService antiSpamService)
        {
            try
            {
                // Create spam check context from the session and message
                SpamCheckContext context = SpamCheckContext.FromSession(e.Session, e.Message);
                context.Subject = e.Message.Subject;
                context.MessageBody = e.Message.TextBody ?? e.Message.HtmlBody;
                context.Recipients = e.Message.Recipients.Select(r => r.Address).ToList();

                // Extract headers
                foreach (KeyValuePair<string, string> header in e.Message.Headers)
                {
                    if (!context.Headers.ContainsKey(header.Key))
                    {
                        context.Headers[header.Key] = [];
                    }
                    context.Headers[header.Key].Add(header.Value);
                }

                // Perform spam check
                AntiSpamResult result = await antiSpamService.CheckAsync(context);

                // Store result for other handlers
                _messageResults[e.Message] = result;

                // Add spam headers to the message
                e.Message.Headers.Add("X-Spam-Check-By", "Zetian.AntiSpam");
                e.Message.Headers.Add("X-Spam-Score", result.TotalScore.ToString("F1"));
                e.Message.Headers.Add("X-Spam-Status", result.IsSpam ? "Yes" : "No");

                if (result.CheckerScores.Count > 0)
                {
                    string scores = string.Join(", ", result.CheckerScores.Select(kvp => $"{kvp.Key}={kvp.Value:F1}"));
                    e.Message.Headers.Add("X-Spam-Checker-Scores", scores);
                }

                // Take action based on result
                switch (result.Action)
                {
                    case SpamAction.Reject:
                        e.Cancel = true;
                        e.Response = new SmtpResponse(
                            result.SmtpResponseCode ?? 550,
                            result.SmtpResponseMessage ?? "Message rejected as spam");
                        break;

                    case SpamAction.Greylist:
                        e.Cancel = true;
                        e.Response = new SmtpResponse(
                            result.SmtpResponseCode ?? 451,
                            result.SmtpResponseMessage ?? "Greylisted, please try again later");
                        break;

                    case SpamAction.Mark:
                        e.Message.Headers.Add("X-Spam", "Yes");
                        if (result.Reasons.Any())
                        {
                            e.Message.Headers.Add("X-Spam-Reasons", string.Join("; ", result.Reasons));
                        }
                        break;

                    case SpamAction.Quarantine:
                        e.Message.Headers.Add("X-Spam-Quarantine", "true");
                        break;
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the message
                Console.WriteLine($"Anti-spam check failed for message {e.Message.Id}: {ex.Message}");
            }
        }

        // Simple content-based spam checker implementation
        private class ContentSpamChecker : ISpamChecker
        {
            private readonly string[] _spamKeywords;
            private readonly double _scorePerKeyword;

            public string Name => "ContentFilter";
            public double Weight => 1.0;
            public bool IsEnabled => true;

            public ContentSpamChecker(string[] spamKeywords, double scorePerKeyword)
            {
                _spamKeywords = spamKeywords;
                _scorePerKeyword = scorePerKeyword;
            }

            public Task<SpamCheckResult> CheckAsync(SpamCheckContext context, CancellationToken cancellationToken = default)
            {
                string content = (context.MessageBody ?? "") + " " + (context.Subject ?? "");
                List<string> foundKeywords = [];

                foreach (string keyword in _spamKeywords)
                {
                    if (content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        foundKeywords.Add(keyword);
                    }
                }

                double score = foundKeywords.Count * _scorePerKeyword;
                SpamCheckResult result = score >= 50
                    ? SpamCheckResult.Spam(Name, score, $"Contains spam keywords: {string.Join(", ", foundKeywords)}")
                    : SpamCheckResult.NotSpam(Name, score);

                return Task.FromResult(result);
            }
        }

        // Subject-based spam checker implementation
        private class SubjectSpamChecker : ISpamChecker
        {
            private readonly string[] _spamPatterns;

            public string Name => "SubjectFilter";
            public double Weight => 1.5;
            public bool IsEnabled => true;

            public SubjectSpamChecker(string[] spamPatterns)
            {
                _spamPatterns = spamPatterns;
            }

            public Task<SpamCheckResult> CheckAsync(SpamCheckContext context, CancellationToken cancellationToken = default)
            {
                if (string.IsNullOrEmpty(context.Subject))
                {
                    return Task.FromResult(SpamCheckResult.NotSpam(Name));
                }

                foreach (string pattern in _spamPatterns)
                {
                    if (Regex.IsMatch(context.Subject, pattern,
                        RegexOptions.IgnoreCase))
                    {
                        return Task.FromResult(SpamCheckResult.Spam(Name, 80,
                            $"Subject matches spam pattern: {pattern}"));
                    }
                }

                return Task.FromResult(SpamCheckResult.NotSpam(Name));
            }
        }
    }
}