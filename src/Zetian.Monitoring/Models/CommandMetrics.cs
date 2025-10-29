using System;

namespace Zetian.Monitoring
{
    /// <summary>
    /// Represents metrics for an SMTP command
    /// </summary>
    public class CommandMetrics
    {
        /// <summary>
        /// Gets or sets the command name
        /// </summary>
        public string Command { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total number of executions
        /// </summary>
        public long TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the number of successful executions
        /// </summary>
        public long SuccessCount { get; set; }

        /// <summary>
        /// Gets or sets the number of failed executions
        /// </summary>
        public long FailureCount { get; set; }

        /// <summary>
        /// Gets the success rate
        /// </summary>
        public double SuccessRate => TotalCount > 0 ? (double)SuccessCount / TotalCount * 100 : 0;

        /// <summary>
        /// Gets or sets the total duration in milliseconds
        /// </summary>
        public double TotalDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the minimum duration in milliseconds
        /// </summary>
        public double MinDurationMs { get; set; } = double.MaxValue;

        /// <summary>
        /// Gets or sets the maximum duration in milliseconds
        /// </summary>
        public double MaxDurationMs { get; set; }

        /// <summary>
        /// Gets the average duration in milliseconds
        /// </summary>
        public double AverageDurationMs => TotalCount > 0 ? TotalDurationMs / TotalCount : 0;

        /// <summary>
        /// Gets or sets the last execution time
        /// </summary>
        public DateTime? LastExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets the P95 duration in milliseconds
        /// </summary>
        public double P95DurationMs { get; set; }

        /// <summary>
        /// Gets or sets the P99 duration in milliseconds
        /// </summary>
        public double P99DurationMs { get; set; }
    }
}