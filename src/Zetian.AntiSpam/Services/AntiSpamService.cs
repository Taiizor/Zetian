using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.AntiSpam.Abstractions;
using Zetian.AntiSpam.Builders;
using Zetian.AntiSpam.Checkers;
using Zetian.AntiSpam.Models;

namespace Zetian.AntiSpam.Services
{
    /// <summary>
    /// Coordinates anti-spam checking across multiple checkers
    /// </summary>
    public class AntiSpamService
    {
        private readonly List<ISpamChecker> _checkers;
        private readonly AntiSpamOptions _options;
        private long _messagesChecked;
        private long _messagesBlocked;

        public AntiSpamService(
            IEnumerable<ISpamChecker> checkers,
            AntiSpamOptions? options = null)
        {
            _checkers = checkers?.ToList() ?? [];
            _options = options ?? new AntiSpamOptions();
        }

        /// <summary>
        /// Checks a message for spam
        /// </summary>
        public async Task<SpamCheckResult> CheckMessageAsync(
            ISmtpMessage message,
            ISmtpSession session,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _messagesChecked);

            List<SpamCheckResult> results = [];
            double totalScore = 0;
            List<string> reasons = [];
            List<string> details = [];

            if (_options.RunChecksInParallel)
            {
                // Run checks in parallel
                IEnumerable<Task<SpamCheckResult>> tasks = _checkers
                    .Where(c => c.IsEnabled)
                    .Select(async checker =>
                    {
                        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        cts.CancelAfter(_options.CheckerTimeout);

                        try
                        {
                            return await checker.CheckAsync(message, session, cts.Token);
                        }
                        catch (Exception ex)
                        {
                            if (_options.EnableDetailedLogging)
                            {
                                Console.WriteLine($"Checker {checker.Name} failed: {ex.Message}");
                            }
                            return SpamCheckResult.Clean(0, $"{checker.Name} check failed");
                        }
                    });

                results.AddRange(await Task.WhenAll(tasks));
            }
            else
            {
                // Run checks sequentially
                foreach (ISpamChecker? checker in _checkers.Where(c => c.IsEnabled))
                {
                    using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(_options.CheckerTimeout);

                    try
                    {
                        SpamCheckResult result = await checker.CheckAsync(message, session, cts.Token);
                        results.Add(result);

                        if (result.IsSpam && !_options.ContinueOnSpamDetection)
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_options.EnableDetailedLogging)
                        {
                            Console.WriteLine($"Checker {checker.Name} failed: {ex.Message}");
                        }
                        results.Add(SpamCheckResult.Clean(0, $"{checker.Name} check failed"));
                    }
                }
            }

            // Aggregate results
            foreach (SpamCheckResult result in results)
            {
                totalScore += result.Score;

                if (result.IsSpam && !string.IsNullOrWhiteSpace(result.Reason))
                {
                    reasons.Add(result.Reason);
                }

                if (!string.IsNullOrWhiteSpace(result.Details))
                {
                    details.Add(result.Details);
                }
            }

            // Normalize score to 0-100 range
            totalScore = Math.Min(totalScore, 100);

            bool isSpam = totalScore >= _options.RejectThreshold ||
                         results.Any(r => r.IsSpam && r.Score >= _options.RejectThreshold);

            if (isSpam)
            {
                Interlocked.Increment(ref _messagesBlocked);
            }

            string combinedReason = reasons.Count > 0
                ? string.Join("; ", reasons)
                : null;

            string combinedDetails = details.Count > 0
                ? string.Join("\n", details)
                : null;

            return isSpam
                ? SpamCheckResult.Spam(totalScore, combinedReason ?? "Multiple spam indicators", combinedDetails)
                : SpamCheckResult.Clean(totalScore, combinedDetails);
        }

        /// <summary>
        /// Gets a specific checker by type
        /// </summary>
        public T? GetChecker<T>() where T : ISpamChecker
        {
            return _checkers.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Gets all enabled checkers
        /// </summary>
        public IReadOnlyList<ISpamChecker> GetCheckers()
        {
            return _checkers.AsReadOnly();
        }

        /// <summary>
        /// Enables or disables a checker by name
        /// </summary>
        public void SetCheckerEnabled(string name, bool enabled)
        {
            ISpamChecker? checker = _checkers.FirstOrDefault(c =>
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (checker != null)
            {
                checker.IsEnabled = enabled;
            }
        }

        /// <summary>
        /// Gets statistics about the service
        /// </summary>
        public AntiSpamServiceStatistics GetStatistics()
        {
            AntiSpamServiceStatistics stats = new()
            {
                MessagesChecked = _messagesChecked,
                MessagesBlocked = _messagesBlocked,
                EnabledCheckers = _checkers.Count(c => c.IsEnabled),
                TotalCheckers = _checkers.Count
            };

            // Get checker-specific stats
            BayesianSpamFilter? bayesian = GetChecker<BayesianSpamFilter>();
            if (bayesian != null)
            {
                BayesianStatistics bayesianStats = bayesian.GetStatistics();
                stats.BayesianStats = new Dictionary<string, object>
                {
                    ["TotalSpam"] = bayesianStats.TotalSpamMessages,
                    ["TotalHam"] = bayesianStats.TotalHamMessages,
                    ["UniqueWords"] = bayesianStats.UniqueWords
                };
            }

            GreylistingChecker? greylisting = GetChecker<GreylistingChecker>();
            if (greylisting != null)
            {
                GreylistStatistics greylistStats = greylisting.GetStatistics();
                stats.GreylistStats = new Dictionary<string, object>
                {
                    ["TotalEntries"] = greylistStats.TotalEntries,
                    ["Greylisted"] = greylistStats.GreylistedEntries,
                    ["Whitelisted"] = greylistStats.WhitelistedEntries
                };
            }

            return stats;
        }

        /// <summary>
        /// Clears all cached data
        /// </summary>
        public void ClearCache()
        {
            GetChecker<BayesianSpamFilter>()?.Clear();
            GetChecker<GreylistingChecker>()?.Clear();

            _messagesChecked = 0;
            _messagesBlocked = 0;
        }
    }

    /// <summary>
    /// Statistics for the anti-spam service
    /// </summary>
    public class AntiSpamServiceStatistics
    {
        public long MessagesChecked { get; set; }
        public long MessagesBlocked { get; set; }
        public int EnabledCheckers { get; set; }
        public int TotalCheckers { get; set; }
        public Dictionary<string, object>? BayesianStats { get; set; }
        public Dictionary<string, object>? GreylistStats { get; set; }

        public double BlockRate => MessagesChecked > 0
            ? (double)MessagesBlocked / MessagesChecked * 100
            : 0;
    }
}