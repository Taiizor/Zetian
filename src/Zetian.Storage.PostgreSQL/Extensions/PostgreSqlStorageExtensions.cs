using Microsoft.Extensions.Logging;
using System;
using Zetian.Server;
using Zetian.Storage.Extensions;

namespace Zetian.Storage.PostgreSQL.Extensions
{
    /// <summary>
    /// Extension methods for configuring PostgreSQL storage
    /// </summary>
    public static class PostgreSqlStorageExtensions
    {
        /// <summary>
        /// Configures PostgreSQL as the message storage provider
        /// </summary>
        public static SmtpServerBuilder WithPostgreSqlStorage(
            this SmtpServerBuilder builder,
            string connectionString,
            Action<PostgreSqlStorageConfiguration>? configure = null)
        {
            PostgreSqlStorageConfiguration configuration = new()
            {
                ConnectionString = connectionString
            };

            configure?.Invoke(configuration);
            configuration.Validate();

            ILogger<PostgreSqlMessageStore>? logger = builder.GetLogger<PostgreSqlMessageStore>();
            PostgreSqlMessageStore store = new(configuration, logger);

            return builder.MessageStore(store);
        }
    }
}