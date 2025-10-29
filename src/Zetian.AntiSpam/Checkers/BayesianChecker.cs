using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Zetian.AntiSpam.Abstractions;
using Zetian.AntiSpam.Models;

namespace Zetian.AntiSpam.Checkers
{
    /// <summary>
    /// Bayesian spam filter using naive Bayes classification
    /// </summary>
    public class BayesianChecker : ISpamChecker
    {
        private readonly ILogger<BayesianChecker>? _logger;
        private readonly BayesianConfiguration _configuration;
        private readonly Dictionary<string, WordStatistics> _wordStats;
        private int _spamCount;
        private int _hamCount;
        private readonly object _lockObject = new();

        public string Name => "Bayesian";
        public double Weight => _configuration.Weight;
        public bool IsEnabled => _configuration.Enabled;

        public BayesianChecker(BayesianConfiguration? configuration = null, ILogger<BayesianChecker>? logger = null)
        {
            _configuration = configuration ?? new BayesianConfiguration();
            _logger = logger;
            _wordStats = new Dictionary<string, WordStatistics>(StringComparer.OrdinalIgnoreCase);

            // Initialize with some common spam indicators
            InitializeDefaultWords();
        }

        public async Task<SpamCheckResult> CheckAsync(SpamCheckContext context, CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
            {
                return SpamCheckResult.NotSpam(Name);
            }

            List<string> tokens = [];

            // Extract tokens from various parts of the email
            if (!string.IsNullOrEmpty(context.Subject))
            {
                tokens.AddRange(Tokenize(context.Subject));
            }

            if (!string.IsNullOrEmpty(context.MessageBody))
            {
                tokens.AddRange(Tokenize(context.MessageBody));
            }

            if (!string.IsNullOrEmpty(context.FromAddress))
            {
                tokens.AddRange(TokenizeEmail(context.FromAddress));
            }

            // Add header-based features
            tokens.AddRange(ExtractHeaderFeatures(context.Headers));

            if (tokens.Count == 0)
            {
                _logger?.LogDebug("No tokens extracted for Bayesian analysis");
                return SpamCheckResult.NotSpam(Name);
            }

            double spamProbability = await Task.Run(() => CalculateSpamProbability(tokens), cancellationToken);

            return BuildResult(spamProbability, tokens);
        }

        /// <summary>
        /// Train the filter with a message
        /// </summary>
        public void Train(string content, bool isSpam)
        {
            List<string> tokens = Tokenize(content);

            lock (_lockObject)
            {
                if (isSpam)
                {
                    _spamCount++;
                }
                else
                {
                    _hamCount++;
                }

                foreach (string token in tokens)
                {
                    if (!_wordStats.ContainsKey(token))
                    {
                        _wordStats[token] = new WordStatistics();
                    }

                    if (isSpam)
                    {
                        _wordStats[token].SpamCount++;
                    }
                    else
                    {
                        _wordStats[token].HamCount++;
                    }
                }
            }
        }

        /// <summary>
        /// Train the filter with multiple messages
        /// </summary>
        public void TrainBatch(IEnumerable<(string content, bool isSpam)> samples)
        {
            foreach ((string? content, bool isSpam) in samples)
            {
                Train(content, isSpam);
            }
        }

        private double CalculateSpamProbability(List<string> tokens)
        {
            if (_spamCount == 0 || _hamCount == 0)
            {
                _logger?.LogDebug("Insufficient training data for Bayesian filter");
                return 0.5; // Neutral probability
            }

            List<double> probabilities = [];

            lock (_lockObject)
            {
                foreach (string? token in tokens.Distinct())
                {
                    if (_wordStats.TryGetValue(token, out WordStatistics? stats))
                    {
                        double wordProbability = CalculateWordSpamProbability(stats);
                        probabilities.Add(wordProbability);
                    }
                }
            }

            if (probabilities.Count == 0)
            {
                return 0.5; // No known words, neutral probability
            }

            // Use only the most significant probabilities
            List<double> significantProbs = probabilities
                .OrderBy(p => Math.Abs(0.5 - p))
                .Take(_configuration.MaxTokensToConsider)
                .ToList();

            // Combine probabilities using the inverse chi-square method
            return CombineProbabilities(significantProbs);
        }

        private double CalculateWordSpamProbability(WordStatistics stats)
        {
            double spamFrequency = (double)stats.SpamCount / Math.Max(1, _spamCount);
            double hamFrequency = (double)stats.HamCount / Math.Max(1, _hamCount);

            // Apply Laplace smoothing to avoid zero probabilities
            double k = _configuration.SmoothingFactor;
            spamFrequency = (stats.SpamCount + k) / (_spamCount + (2 * k));
            hamFrequency = (stats.HamCount + k) / (_hamCount + (2 * k));

            // Calculate probability using Bayes' theorem
            double spamProbability = spamFrequency / (spamFrequency + hamFrequency);

            // Apply bounds to avoid extreme probabilities
            return Math.Max(_configuration.MinProbability,
                   Math.Min(_configuration.MaxProbability, spamProbability));
        }

        private double CombineProbabilities(List<double> probabilities)
        {
            if (probabilities.Count == 0)
            {
                return 0.5;
            }

            // Use Paul Graham's combining formula
            double product = 1.0;
            double inverseProduct = 1.0;

            foreach (double p in probabilities)
            {
                product *= p;
                inverseProduct *= 1.0 - p;
            }

            if (product + inverseProduct == 0)
            {
                return 0.5;
            }

            return product / (product + inverseProduct);
        }

        private List<string> Tokenize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return [];
            }

            // Convert to lowercase and split into words
            List<string> words = Regex.Split(text.ToLowerInvariant(), @"\W+")
                .Where(w => !string.IsNullOrWhiteSpace(w) &&
                           w.Length >= _configuration.MinTokenLength &&
                           w.Length <= _configuration.MaxTokenLength)
                .ToList();

            // Add bi-grams for better accuracy
            if (_configuration.UseBiGrams)
            {
                List<string> biGrams = [];
                for (int i = 0; i < words.Count - 1; i++)
                {
                    biGrams.Add($"{words[i]}_{words[i + 1]}");
                }
                words.AddRange(biGrams);
            }

            return words;
        }

        private List<string> TokenizeEmail(string email)
        {
            List<string> tokens =
            [
                // Add the full email as a token
                $"email:{email.ToLowerInvariant()}",
            ];

            // Add domain as a token
            int atIndex = email.IndexOf('@');
            if (atIndex > 0 && atIndex < email.Length - 1)
            {
                string domain = email[(atIndex + 1)..];
                tokens.Add($"domain:{domain.ToLowerInvariant()}");

                // Add TLD as a token
                int lastDot = domain.LastIndexOf('.');
                if (lastDot > 0 && lastDot < domain.Length - 1)
                {
                    string tld = domain[(lastDot + 1)..];
                    tokens.Add($"tld:{tld.ToLowerInvariant()}");
                }
            }

            return tokens;
        }

        private List<string> ExtractHeaderFeatures(Dictionary<string, List<string>> headers)
        {
            List<string> features = [];

            // Check for suspicious headers
            if (headers.ContainsKey("X-Mailer"))
            {
                string? mailer = headers["X-Mailer"].FirstOrDefault();
                if (!string.IsNullOrEmpty(mailer))
                {
                    features.Add($"mailer:{mailer.ToLowerInvariant()}");
                }
            }

            // Check for missing headers that legitimate mail usually has
            if (!headers.ContainsKey("Message-ID"))
            {
                features.Add("missing:message-id");
            }

            if (!headers.ContainsKey("Date"))
            {
                features.Add("missing:date");
            }

            // Check for multiple "Received" headers (legitimate mail usually has several)
            int receivedCount = headers.ContainsKey("Received") ? headers["Received"].Count : 0;
            if (receivedCount < 2)
            {
                features.Add("few-received-headers");
            }

            // Check for suspicious content-type
            if (headers.TryGetValue("Content-Type", out List<string>? contentTypes))
            {
                string? contentType = contentTypes.FirstOrDefault()?.ToLowerInvariant();
                if (contentType != null)
                {
                    if (contentType.Contains("multipart/alternative") ||
                        contentType.Contains("multipart/mixed"))
                    {
                        features.Add("multipart-message");
                    }
                }
            }

            return features;
        }

        private void InitializeDefaultWords()
        {
            // Initialize with some common spam indicators
            string[] spamWords = new[]
            {
                "viagra", "pills", "medication", "pharmacy", "click here", "act now",
                "limited time", "free", "winner", "congratulations", "urgent",
                "weight loss", "increase sales", "online marketing", "work from home",
                "million dollars", "nigerian prince", "wire transfer", "bitcoin",
                "casino", "lottery", "guaranteed", "100% free", "no obligation"
            };

            string[] hamWords = new[]
            {
                "meeting", "schedule", "project", "report", "update", "team",
                "document", "review", "feedback", "colleague", "office", "work",
                "regards", "sincerely", "thanks", "please", "attached", "follow"
            };

            foreach (string? word in spamWords)
            {
                _wordStats[word] = new WordStatistics { SpamCount = 10, HamCount = 1 };
            }

            foreach (string? word in hamWords)
            {
                _wordStats[word] = new WordStatistics { SpamCount = 1, HamCount = 10 };
            }

            _spamCount = spamWords.Length * 10;
            _hamCount = hamWords.Length * 10;
        }

        private SpamCheckResult BuildResult(double spamProbability, List<string> tokens)
        {
            double score = spamProbability * 100;
            bool isSpam = spamProbability >= _configuration.SpamThreshold;

            SpamCheckResult result = new()
            {
                IsSpam = isSpam,
                Score = score,
                CheckerName = Name,
                Confidence = Math.Abs(spamProbability - 0.5) * 2, // Convert to 0-1 scale
                Details =
                {
                    ["bayesian_probability"] = spamProbability,
                    ["tokens_analyzed"] = tokens.Count,
                    ["training_samples"] = _spamCount + _hamCount
                }
            };

            if (isSpam)
            {
                result.Action = spamProbability >= _configuration.HighConfidenceThreshold
                    ? SpamAction.Reject
                    : SpamAction.Quarantine;

                result.SmtpResponseCode = 550;
                result.SmtpResponseMessage = "Message classified as spam";
                result.Reasons.Add($"Bayesian score: {score:F1}%");

                // Add top spam indicators to reasons
                List<string> topSpamWords = GetTopSpamWords(tokens, 3);
                if (topSpamWords.Any())
                {
                    result.Reasons.Add($"Spam indicators: {string.Join(", ", topSpamWords)}");
                }
            }
            else
            {
                result.Action = SpamAction.None;
            }

            return result;
        }

        private List<string> GetTopSpamWords(List<string> tokens, int count)
        {
            List<(string word, double probability)> spamWords = [];

            lock (_lockObject)
            {
                foreach (string? token in tokens.Distinct())
                {
                    if (_wordStats.TryGetValue(token, out WordStatistics? stats))
                    {
                        double prob = CalculateWordSpamProbability(stats);
                        if (prob > 0.8) // High spam probability
                        {
                            spamWords.Add((token, prob));
                        }
                    }
                }
            }

            return spamWords
                .OrderByDescending(w => w.probability)
                .Take(count)
                .Select(w => w.word)
                .ToList();
        }

        private class WordStatistics
        {
            public int SpamCount { get; set; }
            public int HamCount { get; set; }
        }
    }

    /// <summary>
    /// Configuration for Bayesian spam checking
    /// </summary>
    public class BayesianConfiguration
    {
        public bool Enabled { get; set; } = true;
        public double Weight { get; set; } = 1.2;
        public double SpamThreshold { get; set; } = 0.8;
        public double HighConfidenceThreshold { get; set; } = 0.95;
        public int MinTokenLength { get; set; } = 3;
        public int MaxTokenLength { get; set; } = 20;
        public int MaxTokensToConsider { get; set; } = 15;
        public bool UseBiGrams { get; set; } = true;
        public double SmoothingFactor { get; set; } = 1.0;
        public double MinProbability { get; set; } = 0.01;
        public double MaxProbability { get; set; } = 0.99;
    }
}