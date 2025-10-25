using Microsoft.Extensions.Logging;
using System;
using Zetian.Server;
using Zetian.Storage.Extensions;

namespace Zetian.Storage.SqlServer.Extensions
{
    /// <summary>
    /// Extension methods for configuring SQL Server storage
    /// </summary>
    public static class SqlServerStorageExtensions
    {
        /// <summary>
        /// Configures SQL Server as the message storage provider
        /// </summary>
        public static SmtpServerBuilder WithSqlServerStorage(
            this SmtpServerBuilder builder,
            string connectionString,
            Action<SqlServerStorageConfiguration>? configure = null)
        {
            SqlServerStorageConfiguration configuration = new()
            {
                ConnectionString = connectionString
            };

            configure?.Invoke(configuration);
            configuration.Validate();

            ILogger<SqlServerMessageStore>? logger = builder.GetLogger<SqlServerMessageStore>();
            SqlServerMessageStore store = new(configuration, logger);

            return builder.MessageStore(store);
        }
    }
}