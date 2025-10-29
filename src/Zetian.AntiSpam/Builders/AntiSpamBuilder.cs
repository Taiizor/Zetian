using DnsClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zetian.AntiSpam.Abstractions;
using Zetian.AntiSpam.Checkers;
using Zetian.AntiSpam.Models;
using Zetian.AntiSpam.Services;

namespace Zetian.AntiSpam.Builders
{
    /// <summary>
    /// Builder for configuring and creating an AntiSpam service
    /// </summary>
    public class AntiSpamBuilder
    {
        private readonly List<ISpamChecker> _checkers = [];
        private AntiSpamConfiguration _configuration = new();
        private ILogger<AntiSpamService>? _logger;
        private ILookupClient? _dnsClient;
        private ILoggerFactory? _loggerFactory;

        /// <summary>
        /// Creates a new AntiSpamBuilder instance
        /// </summary>
        public static AntiSpamBuilder Create()
        {
            return new AntiSpamBuilder();
        }

        /// <summary>
        /// Sets the logger factory for all components
        /// </summary>
        public AntiSpamBuilder WithLoggerFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory?.CreateLogger<AntiSpamService>();
            return this;
        }

        /// <summary>
        /// Sets a custom DNS client for DNS-based checks
        /// </summary>
        public AntiSpamBuilder WithDnsClient(ILookupClient dnsClient)
        {
            _dnsClient = dnsClient;
            return this;
        }

        /// <summary>
        /// Configures the anti-spam service
        /// </summary>
        public AntiSpamBuilder Configure(Action<AntiSpamConfiguration> configAction)
        {
            configAction?.Invoke(_configuration);
            return this;
        }

        /// <summary>
        /// Sets the spam threshold score (0-100)
        /// </summary>
        public AntiSpamBuilder WithSpamThreshold(double threshold)
        {
            _configuration.SpamThreshold = Math.Max(0, Math.Min(100, threshold));
            return this;
        }

        /// <summary>
        /// Sets the reject threshold score (0-100)
        /// </summary>
        public AntiSpamBuilder WithRejectThreshold(double threshold)
        {
            _configuration.RejectThreshold = Math.Max(0, Math.Min(100, threshold));
            return this;
        }

        /// <summary>
        /// Enables or disables parallel checking
        /// </summary>
        public AntiSpamBuilder RunInParallel(bool parallel = true)
        {
            _configuration.RunChecksInParallel = parallel;
            return this;
        }

        /// <summary>
        /// Sets the timeout for each checker
        /// </summary>
        public AntiSpamBuilder WithCheckerTimeout(TimeSpan timeout)
        {
            _configuration.CheckerTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Adds SPF (Sender Policy Framework) checking
        /// </summary>
        public AntiSpamBuilder AddSpfChecker(Action<SpfConfiguration>? configAction = null)
        {
            SpfConfiguration config = new();
            configAction?.Invoke(config);

            ILogger<SpfChecker>? logger = _loggerFactory?.CreateLogger<SpfChecker>();
            _checkers.Add(new SpfChecker(config, _dnsClient, logger));

            return this;
        }

        /// <summary>
        /// Adds RBL/DNSBL (Realtime Blackhole List) checking
        /// </summary>
        public AntiSpamBuilder AddRblChecker(Action<RblConfiguration>? configAction = null)
        {
            RblConfiguration config = new();
            configAction?.Invoke(config);

            ILogger<RblChecker>? logger = _loggerFactory?.CreateLogger<RblChecker>();
            _checkers.Add(new RblChecker(config, _dnsClient, logger));

            return this;
        }

        /// <summary>
        /// Adds Bayesian spam filtering
        /// </summary>
        public AntiSpamBuilder AddBayesianChecker(Action<BayesianConfiguration>? configAction = null)
        {
            BayesianConfiguration config = new();
            configAction?.Invoke(config);

            ILogger<BayesianChecker>? logger = _loggerFactory?.CreateLogger<BayesianChecker>();
            _checkers.Add(new BayesianChecker(config, logger));

            return this;
        }

        /// <summary>
        /// Adds a custom spam checker
        /// </summary>
        public AntiSpamBuilder AddCustomChecker(ISpamChecker checker)
        {
            if (checker != null)
            {
                _checkers.Add(checker);
            }
            return this;
        }

        /// <summary>
        /// Adds a simple function-based spam checker
        /// </summary>
        public AntiSpamBuilder AddFunctionChecker(
            string name,
            Func<SpamCheckContext, SpamCheckResult> checkFunction,
            double weight = 1.0,
            bool enabled = true)
        {
            _checkers.Add(new FunctionSpamChecker(name, checkFunction, weight, enabled));
            return this;
        }

        /// <summary>
        /// Adds an async function-based spam checker
        /// </summary>
        public AntiSpamBuilder AddAsyncFunctionChecker(
            string name,
            Func<SpamCheckContext, Task<SpamCheckResult>> checkFunction,
            double weight = 1.0,
            bool enabled = true)
        {
            _checkers.Add(new AsyncFunctionSpamChecker(name, checkFunction, weight, enabled));
            return this;
        }

        /// <summary>
        /// Configures RBL servers to use
        /// </summary>
        public AntiSpamBuilder UseRblServers(params string[] servers)
        {
            return AddRblChecker(config =>
            {
                config.Servers.Clear();
                foreach (string server in servers)
                {
                    config.Servers.Add(new RblServer
                    {
                        Name = server,
                        Host = server,
                        Enabled = true
                    });
                }
            });
        }

        /// <summary>
        /// Adds all default checkers with standard configuration
        /// </summary>
        public AntiSpamBuilder AddDefaultCheckers()
        {
            return this
                .AddSpfChecker()
                .AddRblChecker()
                .AddBayesianChecker();
        }

        /// <summary>
        /// Creates a basic anti-spam configuration for quick setup
        /// </summary>
        public static AntiSpamService CreateBasic()
        {
            return Create()
                .AddDefaultCheckers()
                .WithSpamThreshold(50)
                .WithRejectThreshold(80)
                .Build();
        }

        /// <summary>
        /// Creates a strict anti-spam configuration
        /// </summary>
        public static AntiSpamService CreateStrict()
        {
            return Create()
                .AddDefaultCheckers()
                .WithSpamThreshold(30)
                .WithRejectThreshold(60)
                .Configure(c =>
                {
                    c.StopOnFirstReject = true;
                })
                .Build();
        }

        /// <summary>
        /// Creates a lenient anti-spam configuration
        /// </summary>
        public static AntiSpamService CreateLenient()
        {
            return Create()
                .AddSpfChecker(c => c.SoftFailAsSpam = false)
                .AddBayesianChecker(c => c.SpamThreshold = 0.9)
                .WithSpamThreshold(70)
                .WithRejectThreshold(95)
                .Build();
        }

        /// <summary>
        /// Builds the configured AntiSpam service
        /// </summary>
        public AntiSpamService Build()
        {
            return new AntiSpamService(_checkers, _configuration, _logger);
        }

        // Helper checker implementations for function-based checkers
        private class FunctionSpamChecker(
            string name,
            Func<SpamCheckContext, SpamCheckResult> checkFunction,
            double weight,
            bool enabled) : ISpamChecker
        {
            public string Name { get; } = name;
            public double Weight { get; } = weight;
            public bool IsEnabled { get; } = enabled;

            public Task<SpamCheckResult> CheckAsync(SpamCheckContext context, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(checkFunction(context));
            }
        }

        private class AsyncFunctionSpamChecker(
            string name,
            Func<SpamCheckContext, Task<SpamCheckResult>> checkFunction,
            double weight,
            bool enabled) : ISpamChecker
        {
            public string Name { get; } = name;
            public double Weight { get; } = weight;
            public bool IsEnabled { get; } = enabled;

            public async Task<SpamCheckResult> CheckAsync(SpamCheckContext context, CancellationToken cancellationToken = default)
            {
                return await checkFunction(context);
            }
        }
    }
}