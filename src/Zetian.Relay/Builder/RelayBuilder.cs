using System;
using System.Net;
using Zetian.Relay.Configuration;

namespace Zetian.Relay.Builder
{
    /// <summary>
    /// Builder for configuring relay settings
    /// </summary>
    public class RelayBuilder
    {
        private readonly RelayConfiguration _configuration;

        public RelayBuilder()
        {
            _configuration = new RelayConfiguration();
        }

        /// <summary>
        /// Enables or disables relay
        /// </summary>
        public RelayBuilder Enable(bool enabled = true)
        {
            _configuration.Enabled = enabled;
            return this;
        }

        /// <summary>
        /// Sets the default smart host
        /// </summary>
        public RelayBuilder WithSmartHost(
            string host,
            int port = 25,
            string? username = null,
            string? password = null)
        {
            _configuration.DefaultSmartHost = new SmartHostConfiguration
            {
                Host = host,
                Port = port,
                Credentials = !string.IsNullOrEmpty(username)
                    ? new NetworkCredential(username, password)
                    : null,
                UseTls = port is 465 or 587,
                UseStartTls = port == 587
            };
            return this;
        }

        /// <summary>
        /// Adds an additional smart host for failover
        /// </summary>
        public RelayBuilder AddSmartHost(SmartHostConfiguration smartHost)
        {
            _configuration.SmartHosts.Add(smartHost);
            return this;
        }

        /// <summary>
        /// Sets the maximum concurrent deliveries
        /// </summary>
        public RelayBuilder MaxConcurrentDeliveries(int max)
        {
            _configuration.MaxConcurrentDeliveries = max;
            return this;
        }

        /// <summary>
        /// Sets the maximum retry count
        /// </summary>
        public RelayBuilder MaxRetries(int maxRetries)
        {
            _configuration.MaxRetryCount = maxRetries;
            return this;
        }

        /// <summary>
        /// Sets the message lifetime
        /// </summary>
        public RelayBuilder MessageLifetime(TimeSpan lifetime)
        {
            _configuration.MessageLifetime = lifetime;
            return this;
        }

        /// <summary>
        /// Sets the connection timeout
        /// </summary>
        public RelayBuilder ConnectionTimeout(TimeSpan timeout)
        {
            _configuration.ConnectionTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Enables MX routing
        /// </summary>
        public RelayBuilder UseMxRouting(bool enable = true)
        {
            _configuration.UseMxRouting = enable;
            return this;
        }

        /// <summary>
        /// Adds DNS servers for MX lookups
        /// </summary>
        public RelayBuilder AddDnsServer(params IPAddress[] servers)
        {
            foreach (IPAddress server in servers)
            {
                _configuration.DnsServers.Add(server);
            }
            return this;
        }

        /// <summary>
        /// Enables TLS for outbound connections
        /// </summary>
        public RelayBuilder EnableTls(bool enable = true, bool require = false)
        {
            _configuration.EnableTls = enable;
            _configuration.RequireTls = require;
            return this;
        }

        /// <summary>
        /// Sets the local domain for HELO/EHLO
        /// </summary>
        public RelayBuilder LocalDomain(string domain)
        {
            _configuration.LocalDomain = domain;
            return this;
        }

        /// <summary>
        /// Adds local domains (not relayed)
        /// </summary>
        public RelayBuilder AddLocalDomains(params string[] domains)
        {
            foreach (string domain in domains)
            {
                _configuration.LocalDomains.Add(domain);
            }
            return this;
        }

        /// <summary>
        /// Adds relay domains (always relayed)
        /// </summary>
        public RelayBuilder AddRelayDomains(params string[] domains)
        {
            foreach (string domain in domains)
            {
                _configuration.RelayDomains.Add(domain);
            }
            return this;
        }

        /// <summary>
        /// Adds relay networks (IPs allowed to relay)
        /// </summary>
        public RelayBuilder AddRelayNetworks(params IPAddress[] networks)
        {
            foreach (IPAddress network in networks)
            {
                _configuration.RelayNetworks.Add(network);
            }
            return this;
        }

        /// <summary>
        /// Requires authentication for relay
        /// </summary>
        public RelayBuilder RequireAuthentication(bool require = true)
        {
            _configuration.RequireAuthentication = require;
            return this;
        }

        /// <summary>
        /// Adds domain-specific routing
        /// </summary>
        public RelayBuilder AddDomainRoute(
            string domain,
            string host,
            int port = 25,
            string? username = null,
            string? password = null)
        {
            _configuration.DomainRouting[domain] = new SmartHostConfiguration
            {
                Host = host,
                Port = port,
                Credentials = !string.IsNullOrEmpty(username)
                    ? new NetworkCredential(username, password)
                    : null,
                UseTls = port is 465 or 587,
                UseStartTls = port == 587
            };
            return this;
        }

        /// <summary>
        /// Enables bounce messages
        /// </summary>
        public RelayBuilder EnableBounce(bool enable = true, string? sender = null)
        {
            _configuration.EnableBounceMessages = enable;
            if (!string.IsNullOrEmpty(sender))
            {
                _configuration.BounceSender = sender;
            }
            return this;
        }

        /// <summary>
        /// Enables delivery status notifications
        /// </summary>
        public RelayBuilder EnableDsn(bool enable = true)
        {
            _configuration.EnableDsn = enable;
            return this;
        }

        /// <summary>
        /// Sets the queue processing interval
        /// </summary>
        public RelayBuilder QueueProcessingInterval(TimeSpan interval)
        {
            _configuration.QueueProcessingInterval = interval;
            return this;
        }

        /// <summary>
        /// Sets the cleanup interval
        /// </summary>
        public RelayBuilder CleanupInterval(TimeSpan interval)
        {
            _configuration.CleanupInterval = interval;
            return this;
        }

        /// <summary>
        /// Builds the relay configuration
        /// </summary>
        public RelayConfiguration Build()
        {
            _configuration.Validate();
            return _configuration;
        }
    }
}
