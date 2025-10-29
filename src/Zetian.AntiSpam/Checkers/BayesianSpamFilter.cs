using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.AntiSpam.Abstractions;
using Zetian.AntiSpam.Models;

namespace Zetian.AntiSpam.Checkers
{
    /// <summary>
    /// Bayesian spam filter using statistical analysis
    /// </summary>
    public class BayesianSpamFilter : ISpamChecker
    {
        private readonly ConcurrentDictionary<string, WordStatistics> _wordStats;
        private readonly double _spamThreshold;
        private readonly double _unknownWordProbability;
        private readonly int _minWordLength;
        private readonly int _maxWordLength;
        private long _totalSpamMessages;
        private long _totalHamMessages;
        private readonly object _statsLock = new();

        public BayesianSpamFilter(
            double spamThreshold = 0.9,
            double unknownWordProbability = 0.5,
            int minWordLength = 3,
            int maxWordLength = 50)
        {
            _wordStats = new ConcurrentDictionary<string, WordStatistics>(StringComparer.OrdinalIgnoreCase);
            _spamThreshold = spamThreshold;
            _unknownWordProbability = unknownWordProbability;
            _minWordLength = minWordLength;
            _maxWordLength = maxWordLength;
            IsEnabled = true;
        }

        public string Name => "Bayesian";

        public bool IsEnabled { get; set; }

        public async Task<SpamCheckResult> CheckAsync(
            ISmtpMessage message,
            ISmtpSession session,
            CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
            {
                return SpamCheckResult.Clean(0, "Bayesian filter disabled");
            }

            // Extract content from message
            string content = ExtractContent(message);
            if (string.IsNullOrWhiteSpace(content))
            {
                return SpamCheckResult.Clean(0, "No content to analyze");
            }

            // Tokenize and analyze
            HashSet<string> tokens = Tokenize(content);
            double spamProbability = await CalculateSpamProbabilityAsync(tokens, cancellationToken);
            double score = spamProbability * 100;

            if (spamProbability >= _spamThreshold)
            {
                return SpamCheckResult.Spam(
                    score,
                    $"Bayesian analysis: {spamProbability:P1} spam probability",
                    $"Analyzed {tokens.Count} unique tokens");
            }

            return SpamCheckResult.Clean(score, $"Bayesian score: {spamProbability:P1}");
        }

        /// <summary>
        /// Trains the filter with a spam message
        /// </summary>
        public async Task TrainSpamAsync(string content)
        {
            await TrainAsync(content, true);
        }

        /// <summary>
        /// Trains the filter with a legitimate message (ham)
        /// </summary>
        public async Task TrainHamAsync(string content)
        {
            await TrainAsync(content, false);
        }

        /// <summary>
        /// Trains the filter with a message
        /// </summary>
        public async Task TrainAsync(string content, bool isSpam)
        {
            HashSet<string> tokens = Tokenize(content);

            lock (_statsLock)
            {
                if (isSpam)
                {
                    _totalSpamMessages++;
                }
                else
                {
                    _totalHamMessages++;
                }
            }

            foreach (string token in tokens)
            {
                _wordStats.AddOrUpdate(
                    token,
                    key => new WordStatistics { SpamCount = isSpam ? 1 : 0, HamCount = isSpam ? 0 : 1 },
                    (key, stats) =>
                    {
                        if (isSpam)
                        {
                            stats.SpamCount++;
                        }
                        else
                        {
                            stats.HamCount++;
                        }
                        return stats;
                    });
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Clears all training data
        /// </summary>
        public void Clear()
        {
            _wordStats.Clear();
            lock (_statsLock)
            {
                _totalSpamMessages = 0;
                _totalHamMessages = 0;
            }
        }

        /// <summary>
        /// Gets statistics about the trained model
        /// </summary>
        public BayesianStatistics GetStatistics()
        {
            lock (_statsLock)
            {
                return new BayesianStatistics
                {
                    TotalSpamMessages = _totalSpamMessages,
                    TotalHamMessages = _totalHamMessages,
                    UniqueWords = _wordStats.Count,
                    MostSpammyWords = GetMostSpammyWords(10),
                    MostHammyWords = GetMostHammyWords(10)
                };
            }
        }

        private string ExtractContent(ISmtpMessage message)
        {
            var sb = new StringBuilder();

            // Add subject
            if (!string.IsNullOrWhiteSpace(message.Subject))
            {
                sb.AppendLine(message.Subject);
            }

            // Add from address
            if (message.From != null)
            {
                sb.AppendLine(message.From.ToString());
            }

            // Add text body
            if (!string.IsNullOrWhiteSpace(message.TextBody))
            {
                sb.AppendLine(message.TextBody);
            }

            // Add HTML body (stripped of tags)
            if (!string.IsNullOrWhiteSpace(message.HtmlBody))
            {
                string strippedHtml = StripHtmlTags(message.HtmlBody);
                sb.AppendLine(strippedHtml);
            }

            // Add relevant headers
            foreach (KeyValuePair<string, string> header in message.Headers)
            {
                if (IsRelevantHeader(header.Key))
                {
                    sb.AppendLine($"{header.Key}: {header.Value}");
                }
            }

            return sb.ToString();
        }

        private static bool IsRelevantHeader(string headerName)
        {
            string[] relevantHeaders =
            [
                "X-Mailer", "X-Priority", "X-MSMail-Priority",
                "List-Unsubscribe", "Return-Path", "Reply-To"
            ];

            return relevantHeaders.Contains(headerName, StringComparer.OrdinalIgnoreCase);
        }

        private static string StripHtmlTags(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            // Remove script and style elements
            html = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);

            // Remove HTML tags
            html = Regex.Replace(html, @"<[^>]+>", " ");

            // Decode HTML entities
            html = System.Net.WebUtility.HtmlDecode(html);

            // Remove extra whitespace
            html = Regex.Replace(html, @"\s+", " ");

            return html.Trim();
        }

        private HashSet<string> Tokenize(string content)
        {
            var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Split by word boundaries
            MatchCollection matches = Regex.Matches(content, @"\b[\w']+\b");

            foreach (Match match in matches)
            {
                string word = match.Value.ToLowerInvariant();

                // Apply length filters
                if (word.Length >= _minWordLength && word.Length <= _maxWordLength)
                {
                    tokens.Add(word);
                }
            }

            // Also extract some special patterns
            ExtractSpecialTokens(content, tokens);

            return tokens;
        }

        private static void ExtractSpecialTokens(string content, HashSet<string> tokens)
        {
            // URLs
            MatchCollection urlMatches = Regex.Matches(content, @"https?://[^\s]+", RegexOptions.IgnoreCase);
            foreach (Match match in urlMatches)
            {
                tokens.Add($"URL:{new Uri(match.Value).Host}");
            }

            // Email addresses
            MatchCollection emailMatches = Regex.Matches(content, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b");
            foreach (Match match in emailMatches)
            {
                string[] parts = match.Value.Split('@');
                if (parts.Length == 2)
                {
                    tokens.Add($"EMAIL_DOMAIN:{parts[1].ToLowerInvariant()}");
                }
            }

            // Money amounts
            MatchCollection moneyMatches = Regex.Matches(content, @"\$\d+(?:,\d{3})*(?:\.\d{2})?|\d+(?:,\d{3})*(?:\.\d{2})?\s*(?:USD|EUR|GBP)");
            if (moneyMatches.Count > 0)
            {
                tokens.Add("HAS_MONEY_AMOUNT");
            }

            // Phone numbers
            MatchCollection phoneMatches = Regex.Matches(content, @"\+?\d{1,3}[-.\s]?\(?\d{1,4}\)?[-.\s]?\d{1,4}[-.\s]?\d{1,4}");
            if (phoneMatches.Count > 0)
            {
                tokens.Add("HAS_PHONE_NUMBER");
            }

            // Excessive capitalization
            MatchCollection capsWords = Regex.Matches(content, @"\b[A-Z]{2,}\b");
            if (capsWords.Count > 5)
            {
                tokens.Add("EXCESSIVE_CAPS");
            }

            // Excessive punctuation
            if (Regex.IsMatch(content, @"[!?]{2,}"))
            {
                tokens.Add("EXCESSIVE_PUNCTUATION");
            }
        }

        private async Task<double> CalculateSpamProbabilityAsync(HashSet<string> tokens, CancellationToken cancellationToken)
        {
            if (_totalSpamMessages == 0 || _totalHamMessages == 0)
            {
                // Not enough training data
                return _unknownWordProbability;
            }

            var probabilities = new List<double>();

            foreach (string token in tokens.Take(15)) // Use most significant tokens
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                double wordProbability = GetWordSpamProbability(token);
                probabilities.Add(wordProbability);
            }

            if (probabilities.Count == 0)
            {
                return _unknownWordProbability;
            }

            // Combine probabilities using Bayes' theorem
            return CombineProbabilities(probabilities);
        }

        private double GetWordSpamProbability(string word)
        {
            if (!_wordStats.TryGetValue(word, out WordStatistics? stats))
            {
                return _unknownWordProbability;
            }

            double spamProbability = (double)_totalSpamMessages / (_totalSpamMessages + _totalHamMessages);
            double hamProbability = 1.0 - spamProbability;

            double wordInSpamProbability = (stats.SpamCount + 1.0) / (_totalSpamMessages + 2.0);
            double wordInHamProbability = (stats.HamCount + 1.0) / (_totalHamMessages + 2.0);

            double probability = wordInSpamProbability * spamProbability /
                ((wordInSpamProbability * spamProbability) + (wordInHamProbability * hamProbability));

            // Apply Robinson's technique to reduce impact of rare words
            double s = 0.5; // Assumed probability for unknown words
            double x = 1.0; // Strength of assumption
            double n = stats.SpamCount + stats.HamCount;

            return ((x * s) + (n * probability)) / (x + n);
        }

        private static double CombineProbabilities(List<double> probabilities)
        {
            // Use Robinson's combining technique
            double productSpam = 1.0;
            double productHam = 1.0;

            foreach (double p in probabilities)
            {
                productSpam *= p;
                productHam *= 1.0 - p;
            }

            // Fisher's method
            double hSpam = Math.Pow(productSpam, 1.0 / probabilities.Count);
            double hHam = Math.Pow(productHam, 1.0 / probabilities.Count);

            return hSpam / (hSpam + hHam);
        }

        private List<KeyValuePair<string, double>> GetMostSpammyWords(int count)
        {
            return _wordStats
                .Where(kvp => kvp.Value.SpamCount > 0)
                .Select(kvp => new KeyValuePair<string, double>(
                    kvp.Key,
                    GetWordSpamProbability(kvp.Key)))
                .OrderByDescending(kvp => kvp.Value)
                .Take(count)
                .ToList();
        }

        private List<KeyValuePair<string, double>> GetMostHammyWords(int count)
        {
            return _wordStats
                .Where(kvp => kvp.Value.HamCount > 0)
                .Select(kvp => new KeyValuePair<string, double>(
                    kvp.Key,
                    GetWordSpamProbability(kvp.Key)))
                .OrderBy(kvp => kvp.Value)
                .Take(count)
                .ToList();
        }

        private class WordStatistics
        {
            public long SpamCount { get; set; }
            public long HamCount { get; set; }
        }
    }

    /// <summary>
    /// Statistics about the Bayesian filter
    /// </summary>
    public class BayesianStatistics
    {
        public long TotalSpamMessages { get; set; }
        public long TotalHamMessages { get; set; }
        public int UniqueWords { get; set; }
        public List<KeyValuePair<string, double>> MostSpammyWords { get; set; } = [];
        public List<KeyValuePair<string, double>> MostHammyWords { get; set; } = [];
    }
}