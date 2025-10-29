using Microsoft.Extensions.Logging;
using System;
using Zetian.AntiSpam.Builders;
using Zetian.AntiSpam.Checkers;
using Zetian.AntiSpam.Services;
using Zetian.Server;

namespace Zetian.AntiSpam.Extensions
{
    /// <summary>
    /// Extension methods for integrating AntiSpam with SmtpServerBuilder
    /// </summary>
    public static class SmtpServerBuilderExtensions
    {
        /// <summary>
        /// Adds anti-spam protection to the SMTP server
        /// </summary>
        public static SmtpServerBuilder WithAntiSpam(
            this SmtpServerBuilder builder,
            Action<AntiSpamBuilder>? configureAction = null)
        {
            AntiSpamBuilder antiSpamBuilder = AntiSpamBuilder.Create();

            // Add default checkers if no configuration provided
            if (configureAction == null)
            {
                antiSpamBuilder.AddDefaultCheckers();
            }
            else
            {
                configureAction(antiSpamBuilder);
            }

            AntiSpamService antiSpamService = antiSpamBuilder.Build();

            // Store the service in the builder's service collection
            builder.AddService(antiSpamService);

            return builder;
        }

        /// <summary>
        /// Adds basic anti-spam protection with default settings
        /// </summary>
        public static SmtpServerBuilder WithBasicAntiSpam(this SmtpServerBuilder builder)
        {
            AntiSpamService antiSpamService = AntiSpamBuilder.CreateBasic();
            builder.AddService(antiSpamService);
            return builder;
        }

        /// <summary>
        /// Adds strict anti-spam protection
        /// </summary>
        public static SmtpServerBuilder WithStrictAntiSpam(this SmtpServerBuilder builder)
        {
            AntiSpamService antiSpamService = AntiSpamBuilder.CreateStrict();
            builder.AddService(antiSpamService);
            return builder;
        }

        /// <summary>
        /// Adds lenient anti-spam protection
        /// </summary>
        public static SmtpServerBuilder WithLenientAntiSpam(this SmtpServerBuilder builder)
        {
            AntiSpamService antiSpamService = AntiSpamBuilder.CreateLenient();
            builder.AddService(antiSpamService);
            return builder;
        }

        /// <summary>
        /// Adds SPF checking to the SMTP server
        /// </summary>
        public static SmtpServerBuilder WithSpfCheck(
            this SmtpServerBuilder builder,
            Action<SpfConfiguration>? configureAction = null)
        {
            return builder.WithAntiSpam(antiSpam =>
            {
                antiSpam.AddSpfChecker(configureAction);
            });
        }

        /// <summary>
        /// Adds RBL/DNSBL checking to the SMTP server
        /// </summary>
        public static SmtpServerBuilder WithRblCheck(
            this SmtpServerBuilder builder,
            params string[] rblServers)
        {
            return builder.WithAntiSpam(antiSpam =>
            {
                if (rblServers?.Length > 0)
                {
                    antiSpam.UseRblServers(rblServers);
                }
                else
                {
                    antiSpam.AddRblChecker();
                }
            });
        }

        /// <summary>
        /// Adds Bayesian spam filtering to the SMTP server
        /// </summary>
        public static SmtpServerBuilder WithBayesianFilter(
            this SmtpServerBuilder builder,
            Action<BayesianConfiguration>? configureAction = null)
        {
            return builder.WithAntiSpam(antiSpam =>
            {
                antiSpam.AddBayesianChecker(configureAction);
            });
        }

        /// <summary>
        /// Configures anti-spam with a custom logger factory
        /// </summary>
        public static SmtpServerBuilder WithAntiSpamLogging(
            this SmtpServerBuilder builder,
            ILoggerFactory loggerFactory)
        {
            return builder.WithAntiSpam(antiSpam =>
            {
                antiSpam.WithLoggerFactory(loggerFactory);
                antiSpam.AddDefaultCheckers();
            });
        }
    }
}