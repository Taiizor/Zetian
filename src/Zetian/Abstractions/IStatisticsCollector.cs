using System;

namespace Zetian.Abstractions
{
    /// <summary>
    /// Interface for statistics collection
    /// </summary>
    public interface IStatisticsCollector
    {
        /// <summary>
        /// Records a new session
        /// </summary>
        void RecordSession();

        /// <summary>
        /// Records a received message
        /// </summary>
        void RecordMessage(ISmtpMessage message);

        /// <summary>
        /// Records the specified exception for error tracking or logging purposes.
        /// </summary>
        /// <remarks>Use this method to capture and persist error information for diagnostics or
        /// monitoring. The recorded exception may be used for later analysis or reporting. This method does not throw
        /// exceptions for typical usage; ensure that the provided exception is valid and contains relevant error
        /// details.</remarks>
        /// <param name="exception">The exception to record. Cannot be null.</param>
        void RecordError(Exception exception);

        /// <summary>
        /// Gets the total number of sessions that have been recorded.
        /// </summary>
        long TotalSessions { get; }

        /// <summary>
        /// Gets the total number of messages processed by the instance.
        /// </summary>
        long TotalMessages { get; }

        /// <summary>
        /// Gets the total number of errors that have occurred during the operation.
        /// </summary>
        long TotalErrors { get; }

        /// <summary>
        /// Gets the total number of bytes processed by the instance.
        /// </summary>
        long TotalBytes { get; }
    }
}