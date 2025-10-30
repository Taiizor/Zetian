using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Clustering.Models.EventArgs;
using Zetian.Clustering.Network;

namespace Zetian.Clustering.Abstractions
{
    /// <summary>
    /// Interface for cluster network communication
    /// </summary>
    public interface IClusterNetworkClient
    {
        /// <summary>
        /// Send message to a specific node
        /// </summary>
        Task<AcknowledgmentMessage?> SendMessageAsync(IPEndPoint endpoint, ClusterMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Broadcast message to all nodes
        /// </summary>
        Task BroadcastMessageAsync(ClusterMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send data to a node
        /// </summary>
        Task<bool> SendDataAsync(IPEndPoint endpoint, byte[] data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Start listening for messages
        /// </summary>
        Task StartListeningAsync(int port, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stop listening
        /// </summary>
        Task StopListeningAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Message received event
        /// </summary>
        event EventHandler<MessageReceivedEventArgs>? MessageReceived;
    }
}