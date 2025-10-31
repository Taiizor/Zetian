using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.Configuration;
using Zetian.WebUI.Controllers;

namespace Zetian.WebUI.Services
{
    /// <summary>
    /// Implementation of configuration service
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly ISmtpServer _smtpServer;
        private readonly List<ConfigurationHistoryItem> _history = [];
        private readonly object _lockObject = new();

        public ConfigurationService(ISmtpServer smtpServer)
        {
            _smtpServer = smtpServer;
        }

        public Task<ConfigurationDto> GetConfigurationAsync()
        {
            SmtpServerConfiguration config = _smtpServer.Configuration;
            return Task.FromResult(ConfigurationDto.FromConfiguration(config));
        }

        public Task<ConfigurationUpdateResult> UpdateConfigurationAsync(ConfigurationUpdateRequest request)
        {
            SmtpServerConfiguration config = _smtpServer.Configuration;
            List<string> changedProperties = [];
            bool requiresRestart = false;

            try
            {
                // Apply changes
                if (request.Port.HasValue && request.Port.Value != config.Port)
                {
                    config.Port = request.Port.Value;
                    changedProperties.Add(nameof(config.Port));
                    requiresRestart = true;
                }

                if (!string.IsNullOrEmpty(request.ServerName) && request.ServerName != config.ServerName)
                {
                    config.ServerName = request.ServerName;
                    changedProperties.Add(nameof(config.ServerName));
                }

                if (request.MaxMessageSize.HasValue && request.MaxMessageSize.Value != config.MaxMessageSize)
                {
                    config.MaxMessageSize = request.MaxMessageSize.Value;
                    changedProperties.Add(nameof(config.MaxMessageSize));
                }

                if (request.MaxRecipients.HasValue && request.MaxRecipients.Value != config.MaxRecipients)
                {
                    config.MaxRecipients = request.MaxRecipients.Value;
                    changedProperties.Add(nameof(config.MaxRecipients));
                }

                if (request.MaxConnections.HasValue && request.MaxConnections.Value != config.MaxConnections)
                {
                    config.MaxConnections = request.MaxConnections.Value;
                    changedProperties.Add(nameof(config.MaxConnections));
                }

                if (request.MaxConnectionsPerIp.HasValue && request.MaxConnectionsPerIp.Value != config.MaxConnectionsPerIp)
                {
                    config.MaxConnectionsPerIp = request.MaxConnectionsPerIp.Value;
                    changedProperties.Add(nameof(config.MaxConnectionsPerIp));
                }

                if (request.ConnectionTimeoutSeconds.HasValue)
                {
                    TimeSpan timeout = TimeSpan.FromSeconds(request.ConnectionTimeoutSeconds.Value);
                    if (timeout != config.ConnectionTimeout)
                    {
                        config.ConnectionTimeout = timeout;
                        changedProperties.Add(nameof(config.ConnectionTimeout));
                    }
                }

                if (request.CommandTimeoutSeconds.HasValue)
                {
                    TimeSpan timeout = TimeSpan.FromSeconds(request.CommandTimeoutSeconds.Value);
                    if (timeout != config.CommandTimeout)
                    {
                        config.CommandTimeout = timeout;
                        changedProperties.Add(nameof(config.CommandTimeout));
                    }
                }

                if (request.DataTimeoutSeconds.HasValue)
                {
                    TimeSpan timeout = TimeSpan.FromSeconds(request.DataTimeoutSeconds.Value);
                    if (timeout != config.DataTimeout)
                    {
                        config.DataTimeout = timeout;
                        changedProperties.Add(nameof(config.DataTimeout));
                    }
                }

                if (request.MaxRetryCount.HasValue && request.MaxRetryCount.Value != config.MaxRetryCount)
                {
                    config.MaxRetryCount = request.MaxRetryCount.Value;
                    changedProperties.Add(nameof(config.MaxRetryCount));
                }

                if (request.RequireAuthentication.HasValue && request.RequireAuthentication.Value != config.RequireAuthentication)
                {
                    config.RequireAuthentication = request.RequireAuthentication.Value;
                    changedProperties.Add(nameof(config.RequireAuthentication));
                    requiresRestart = true;
                }

                if (request.RequireSecureConnection.HasValue && request.RequireSecureConnection.Value != config.RequireSecureConnection)
                {
                    config.RequireSecureConnection = request.RequireSecureConnection.Value;
                    changedProperties.Add(nameof(config.RequireSecureConnection));
                    requiresRestart = true;
                }

                if (request.EnableSmtpUtf8.HasValue && request.EnableSmtpUtf8.Value != config.EnableSmtpUtf8)
                {
                    config.EnableSmtpUtf8 = request.EnableSmtpUtf8.Value;
                    changedProperties.Add(nameof(config.EnableSmtpUtf8));
                }

                if (request.EnablePipelining.HasValue && request.EnablePipelining.Value != config.EnablePipelining)
                {
                    config.EnablePipelining = request.EnablePipelining.Value;
                    changedProperties.Add(nameof(config.EnablePipelining));
                }

                if (request.Enable8BitMime.HasValue && request.Enable8BitMime.Value != config.Enable8BitMime)
                {
                    config.Enable8BitMime = request.Enable8BitMime.Value;
                    changedProperties.Add(nameof(config.Enable8BitMime));
                }

                // Validate configuration
                config.Validate();

                // Add to history
                if (changedProperties.Any())
                {
                    AddToHistory("Update", changedProperties);
                }

                return Task.FromResult(new ConfigurationUpdateResult
                {
                    Success = true,
                    RequiresRestart = requiresRestart,
                    ChangedProperties = changedProperties
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ConfigurationUpdateResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        public Task<ValidationResult> ValidateConfigurationAsync(ConfigurationUpdateRequest request)
        {
            List<ValidationError> errors = [];

            if (request.Port.HasValue && (request.Port.Value < 1 || request.Port.Value > 65535))
            {
                errors.Add(new ValidationError
                {
                    Property = nameof(request.Port),
                    Message = "Port must be between 1 and 65535"
                });
            }

            if (request.MaxMessageSize.HasValue && request.MaxMessageSize.Value < 1)
            {
                errors.Add(new ValidationError
                {
                    Property = nameof(request.MaxMessageSize),
                    Message = "MaxMessageSize must be greater than 0"
                });
            }

            if (request.MaxRecipients.HasValue && request.MaxRecipients.Value < 1)
            {
                errors.Add(new ValidationError
                {
                    Property = nameof(request.MaxRecipients),
                    Message = "MaxRecipients must be greater than 0"
                });
            }

            if (request.MaxConnections.HasValue && request.MaxConnections.Value < 1)
            {
                errors.Add(new ValidationError
                {
                    Property = nameof(request.MaxConnections),
                    Message = "MaxConnections must be greater than 0"
                });
            }

            if (request.MaxConnectionsPerIp.HasValue && request.MaxConnectionsPerIp.Value < 1)
            {
                errors.Add(new ValidationError
                {
                    Property = nameof(request.MaxConnectionsPerIp),
                    Message = "MaxConnectionsPerIp must be greater than 0"
                });
            }

            if (request.MaxRetryCount.HasValue && request.MaxRetryCount.Value < 0)
            {
                errors.Add(new ValidationError
                {
                    Property = nameof(request.MaxRetryCount),
                    Message = "MaxRetryCount cannot be negative"
                });
            }

            return Task.FromResult(new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            });
        }

        public async Task<byte[]> ExportConfigurationAsync(string format = "json")
        {
            ConfigurationDto config = await GetConfigurationAsync();

            string content = format.ToLower() switch
            {
                "json" => JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }),
                "xml" => ConvertToXml(config),
                _ => config.ToString() ?? ""
            };

            return Encoding.UTF8.GetBytes(content);
        }

        public async Task<ConfigurationUpdateResult> ImportConfigurationAsync(byte[] data, string format = "json")
        {
            try
            {
                string content = Encoding.UTF8.GetString(data);
                ConfigurationDto? imported = null;

                if (format.ToLower() == "json")
                {
                    imported = JsonSerializer.Deserialize<ConfigurationDto>(content);
                }
                // Add XML support if needed

                if (imported == null)
                {
                    return new ConfigurationUpdateResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to parse configuration"
                    };
                }

                // Convert to update request
                ConfigurationUpdateRequest request = new()
                {
                    Port = imported.Port,
                    ServerName = imported.ServerName,
                    MaxMessageSize = imported.MaxMessageSize,
                    MaxRecipients = imported.MaxRecipients,
                    MaxConnections = imported.MaxConnections,
                    MaxConnectionsPerIp = imported.MaxConnectionsPerIp,
                    ConnectionTimeoutSeconds = (int)imported.ConnectionTimeout.TotalSeconds,
                    CommandTimeoutSeconds = (int)imported.CommandTimeout.TotalSeconds,
                    DataTimeoutSeconds = (int)imported.DataTimeout.TotalSeconds,
                    MaxRetryCount = imported.MaxRetryCount,
                    RequireAuthentication = imported.RequireAuthentication,
                    RequireSecureConnection = imported.RequireSecureConnection,
                    EnableSmtpUtf8 = imported.EnableSmtpUtf8,
                    EnablePipelining = imported.EnablePipelining,
                    Enable8BitMime = imported.Enable8BitMime
                };

                ConfigurationUpdateResult result = await UpdateConfigurationAsync(request);

                if (result.Success)
                {
                    AddToHistory("Import", result.ChangedProperties);
                }

                return result;
            }
            catch (Exception ex)
            {
                return new ConfigurationUpdateResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public Task<ConfigurationUpdateResult> ResetToDefaultsAsync()
        {
            SmtpServerConfiguration defaultConfig = new();

            // Convert to update request
            ConfigurationUpdateRequest request = new()
            {
                Port = defaultConfig.Port,
                ServerName = defaultConfig.ServerName,
                MaxMessageSize = defaultConfig.MaxMessageSize,
                MaxRecipients = defaultConfig.MaxRecipients,
                MaxConnections = defaultConfig.MaxConnections,
                MaxConnectionsPerIp = defaultConfig.MaxConnectionsPerIp,
                ConnectionTimeoutSeconds = (int)defaultConfig.ConnectionTimeout.TotalSeconds,
                CommandTimeoutSeconds = (int)defaultConfig.CommandTimeout.TotalSeconds,
                DataTimeoutSeconds = (int)defaultConfig.DataTimeout.TotalSeconds,
                MaxRetryCount = defaultConfig.MaxRetryCount,
                RequireAuthentication = defaultConfig.RequireAuthentication,
                RequireSecureConnection = defaultConfig.RequireSecureConnection,
                EnableSmtpUtf8 = defaultConfig.EnableSmtpUtf8,
                EnablePipelining = defaultConfig.EnablePipelining,
                Enable8BitMime = defaultConfig.Enable8BitMime
            };

            AddToHistory("Reset", ["All settings"]);

            return UpdateConfigurationAsync(request);
        }

        public Task<IEnumerable<ConfigurationHistoryItem>> GetHistoryAsync()
        {
            lock (_lockObject)
            {
                return Task.FromResult(_history
                    .OrderByDescending(h => h.Timestamp)
                    .Take(100)
                    .AsEnumerable());
            }
        }

        private void AddToHistory(string action, List<string> changedProperties)
        {
            lock (_lockObject)
            {
                ConfigurationHistoryItem historyItem = new()
                {
                    Timestamp = DateTime.UtcNow,
                    User = "admin", // In production, get from authentication context
                    Action = action,
                    Changes = changedProperties.ToDictionary(p => p, p => (object)true)
                };

                _history.Add(historyItem);

                // Keep only last 1000 items
                while (_history.Count > 1000)
                {
                    _history.RemoveAt(0);
                }
            }
        }

        private string ConvertToXml(ConfigurationDto config)
        {
            // Simple XML conversion
            StringBuilder sb = new();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<Configuration>");
            sb.AppendLine($"  <Port>{config.Port}</Port>");
            sb.AppendLine($"  <ServerName>{config.ServerName}</ServerName>");
            sb.AppendLine($"  <MaxMessageSize>{config.MaxMessageSize}</MaxMessageSize>");
            sb.AppendLine($"  <MaxRecipients>{config.MaxRecipients}</MaxRecipients>");
            sb.AppendLine($"  <MaxConnections>{config.MaxConnections}</MaxConnections>");
            sb.AppendLine($"  <MaxConnectionsPerIp>{config.MaxConnectionsPerIp}</MaxConnectionsPerIp>");
            sb.AppendLine($"  <ConnectionTimeout>{config.ConnectionTimeout}</ConnectionTimeout>");
            sb.AppendLine($"  <CommandTimeout>{config.CommandTimeout}</CommandTimeout>");
            sb.AppendLine($"  <DataTimeout>{config.DataTimeout}</DataTimeout>");
            sb.AppendLine($"  <MaxRetryCount>{config.MaxRetryCount}</MaxRetryCount>");
            sb.AppendLine($"  <RequireAuthentication>{config.RequireAuthentication}</RequireAuthentication>");
            sb.AppendLine($"  <RequireSecureConnection>{config.RequireSecureConnection}</RequireSecureConnection>");
            sb.AppendLine($"  <EnableSmtpUtf8>{config.EnableSmtpUtf8}</EnableSmtpUtf8>");
            sb.AppendLine($"  <EnablePipelining>{config.EnablePipelining}</EnablePipelining>");
            sb.AppendLine($"  <Enable8BitMime>{config.Enable8BitMime}</Enable8BitMime>");
            sb.AppendLine("</Configuration>");
            return sb.ToString();
        }
    }
}