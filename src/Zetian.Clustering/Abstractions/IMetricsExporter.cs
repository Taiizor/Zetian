using System.Threading;
using System.Threading.Tasks;
using Zetian.Clustering.Models;

namespace Zetian.Clustering.Abstractions
{
    /// <summary>
    /// Interface for exporting cluster metrics
    /// </summary>
    public interface IMetricsExporter
    {
        /// <summary>
        /// Exporter name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Whether the exporter is enabled
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Exports cluster metrics
        /// </summary>
        Task ExportAsync(ClusterMetrics metrics, CancellationToken cancellationToken = default);

        /// <summary>
        /// Exports node health metrics
        /// </summary>
        Task ExportHealthAsync(ClusterHealth health, CancellationToken cancellationToken = default);

        /// <summary>
        /// Flushes any buffered metrics
        /// </summary>
        Task FlushAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts the exporter
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the exporter
        /// </summary>
        Task StopAsync(CancellationToken cancellationToken = default);
    }
}