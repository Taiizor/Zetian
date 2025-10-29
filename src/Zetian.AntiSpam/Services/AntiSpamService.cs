using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zetian.AntiSpam.Abstractions;
using Zetian.AntiSpam.Models;

namespace Zetian.AntiSpam.Services
{
    /// <summary>
    /// Main anti-spam service that orchestrates multiple spam checkers
    /// </summary>
    public class AntiSpamService
    {
        private readonly List<ISpamChecker> _checkers;
        private readonly AntiSpamConfiguration _configuration;
        private readonly ILogger<AntiSpamService>? _logger;

        public AntiSpamService(
            IEnumerable<ISpamChecker> checkers,
            AntiSpamConfiguration? configuration = null,
            ILogger<AntiSpamService>? logger = null)
        {
            _checkers = checkers?.ToList() ?? [];
            _configuration = configuration ?? new AntiSpamConfiguration();
            _logger = logger;
        }

        /// <summary>
        /// Checks if a message is spam using all configured checkers
        /// </summary>
        public async Task<AntiSpamResult> CheckAsync(SpamCheckContext context, CancellationToken cancellationToken = default)
        {
            if (!_configuration.Enabled)
            {
                _logger?.LogDebug("AntiSpam service is disabled");
                return new AntiSpamResult { IsSpam = false, Action = SpamAction.None };
            }

            List<ISpamChecker> enabledCheckers = _checkers.Where(c => c.IsEnabled).ToList();

            if (!enabledCheckers.Any())
            {
                _logger?.LogWarning("No enabled spam checkers configured");
                return new AntiSpamResult { IsSpam = false, Action = SpamAction.None };
            }

            List<Task<SpamCheckResult>> checkTasks = [];

            // Run checks in parallel or sequentially based on configuration
            if (_configuration.RunChecksInParallel)
            {
                foreach (ISpamChecker checker in enabledCheckers)
                {
                    checkTasks.Add(RunCheckerAsync(checker, context, cancellationToken));
                }

                await Task.WhenAll(checkTasks);
            }
            else
            {
                foreach (ISpamChecker checker in enabledCheckers)
                {
                    // Check if we should stop early
                    if (_configuration.StopOnFirstReject && checkTasks.Any(t => t.IsCompleted && t.Result.Action == SpamAction.Reject))
                    {
                        break;
                    }

                    checkTasks.Add(RunCheckerAsync(checker, context, cancellationToken));
                    await checkTasks.Last();
                }
            }

            List<SpamCheckResult> results = checkTasks.Select(t => t.Result).ToList();
            return AggregateResults(results, context);
        }

        private async Task<SpamCheckResult> RunCheckerAsync(ISpamChecker checker, SpamCheckContext context, CancellationToken cancellationToken)
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_configuration.CheckerTimeout);

                DateTime startTime = DateTime.UtcNow;
                SpamCheckResult result = await checker.CheckAsync(context, cts.Token);
                result.Duration = DateTime.UtcNow - startTime;

                _logger?.LogDebug("Checker {Checker} completed in {Duration}ms with score {Score}",
                    checker.Name, result.Duration.TotalMilliseconds, result.Score);

                return result;
            }
            catch (OperationCanceledException)
            {
                _logger?.LogWarning("Checker {Checker} timed out", checker.Name);
                return new SpamCheckResult
                {
                    CheckerName = checker.Name,
                    IsSpam = false,
                    Score = 0,
                    Action = SpamAction.None,
                    Details = { ["error"] = "timeout" }
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Checker {Checker} failed", checker.Name);
                return new SpamCheckResult
                {
                    CheckerName = checker.Name,
                    IsSpam = false,
                    Score = 0,
                    Action = SpamAction.None,
                    Details = { ["error"] = ex.Message }
                };
            }
        }

        private AntiSpamResult AggregateResults(List<SpamCheckResult> results, SpamCheckContext context)
        {
            AntiSpamResult aggregateResult = new()
            {
                CheckResults = results,
                CheckedAt = DateTime.UtcNow
            };

            if (!results.Any())
            {
                aggregateResult.IsSpam = false;
                aggregateResult.Action = SpamAction.None;
                return aggregateResult;
            }

            // Calculate weighted score
            double totalWeight = results.Sum(r => GetCheckerWeight(r.CheckerName));
            double weightedScore = 0.0;

            foreach (SpamCheckResult result in results)
            {
                double weight = GetCheckerWeight(result.CheckerName);
                weightedScore += result.Score * weight;

                // Collect all reasons
                aggregateResult.Reasons.AddRange(result.Reasons);
            }

            if (totalWeight > 0)
            {
                aggregateResult.TotalScore = weightedScore / totalWeight;
            }

            // Determine overall spam status
            DetermineSpamStatus(aggregateResult, results);

            // Log the final decision
            LogDecision(aggregateResult, context);

            return aggregateResult;
        }

        private void DetermineSpamStatus(AntiSpamResult result, List<SpamCheckResult> checkResults)
        {
            // Check for any hard rejections
            bool hasHardReject = checkResults.Any(r => r.Action == SpamAction.Reject && r.Confidence >= 0.8);
            if (hasHardReject)
            {
                result.IsSpam = true;
                result.Action = SpamAction.Reject;
                result.SmtpResponseCode = 550;
                result.SmtpResponseMessage = "Message rejected as spam";
                return;
            }

            // Check overall score threshold
            if (result.TotalScore >= _configuration.SpamThreshold)
            {
                result.IsSpam = true;

                // Determine action based on score ranges
                if (result.TotalScore >= _configuration.RejectThreshold)
                {
                    result.Action = SpamAction.Reject;
                    result.SmtpResponseCode = 550;
                    result.SmtpResponseMessage = $"Message rejected (spam score: {result.TotalScore:F1})";
                }
                else if (result.TotalScore >= _configuration.QuarantineThreshold)
                {
                    result.Action = SpamAction.Quarantine;
                }
                else
                {
                    result.Action = SpamAction.Mark;
                }
            }
            else
            {
                result.IsSpam = false;
                result.Action = SpamAction.None;
            }

            // Check for greylisting
            SpamCheckResult? greylistResult = checkResults.FirstOrDefault(r => r.Action == SpamAction.Greylist);
            if (greylistResult != null && !result.IsSpam)
            {
                result.Action = SpamAction.Greylist;
                result.SmtpResponseCode = greylistResult.SmtpResponseCode;
                result.SmtpResponseMessage = greylistResult.SmtpResponseMessage;
            }

            // Calculate overall confidence
            result.Confidence = checkResults.Any()
                ? checkResults.Average(r => r.Confidence)
                : 0;
        }

        private double GetCheckerWeight(string checkerName)
        {
            ISpamChecker? checker = _checkers.FirstOrDefault(c => c.Name == checkerName);
            return checker?.Weight ?? 1.0;
        }

        private void LogDecision(AntiSpamResult result, SpamCheckContext context)
        {
            if (result.IsSpam)
            {
                _logger?.LogWarning(
                    "Message classified as spam. From: {From}, Score: {Score}, Action: {Action}, Reasons: {Reasons}",
                    context.FromAddress,
                    result.TotalScore,
                    result.Action,
                    string.Join("; ", result.Reasons));
            }
            else
            {
                _logger?.LogDebug(
                    "Message passed spam checks. From: {From}, Score: {Score}",
                    context.FromAddress,
                    result.TotalScore);
            }
        }

        /// <summary>
        /// Adds a spam checker to the service
        /// </summary>
        public void AddChecker(ISpamChecker checker)
        {
            if (checker != null && !_checkers.Any(c => c.Name == checker.Name))
            {
                _checkers.Add(checker);
            }
        }

        /// <summary>
        /// Removes a spam checker from the service
        /// </summary>
        public void RemoveChecker(string checkerName)
        {
            _checkers.RemoveAll(c => c.Name == checkerName);
        }

        /// <summary>
        /// Gets all configured checkers
        /// </summary>
        public IReadOnlyList<ISpamChecker> GetCheckers()
        {
            return _checkers.AsReadOnly();
        }
    }

    /// <summary>
    /// Configuration for the anti-spam service
    /// </summary>
    public class AntiSpamConfiguration
    {
        public bool Enabled { get; set; } = true;
        public double SpamThreshold { get; set; } = 50.0;
        public double QuarantineThreshold { get; set; } = 60.0;
        public double RejectThreshold { get; set; } = 80.0;
        public bool RunChecksInParallel { get; set; } = true;
        public bool StopOnFirstReject { get; set; } = false;
        public TimeSpan CheckerTimeout { get; set; } = TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// Aggregated result from all spam checkers
    /// </summary>
    public class AntiSpamResult
    {
        public bool IsSpam { get; set; }
        public double TotalScore { get; set; }
        public double Confidence { get; set; }
        public SpamAction Action { get; set; } = SpamAction.None;
        public List<SpamCheckResult> CheckResults { get; set; } = [];
        public List<string> Reasons { get; set; } = [];
        public int? SmtpResponseCode { get; set; }
        public string? SmtpResponseMessage { get; set; }
        public DateTime CheckedAt { get; set; }

        /// <summary>
        /// Gets a summary of which checkers flagged the message
        /// </summary>
        public Dictionary<string, bool> CheckerFlags => CheckResults.ToDictionary(
                    r => r.CheckerName,
                    r => r.IsSpam
                );

        /// <summary>
        /// Gets individual checker scores
        /// </summary>
        public Dictionary<string, double> CheckerScores => CheckResults.ToDictionary(
                    r => r.CheckerName,
                    r => r.Score
                );
    }
}