using System.Net;
using Zetian.Clustering.Network;

namespace Zetian.Clustering.Models.EventArgs
{
    /// <summary>
    /// Message received event args
    /// </summary>
    public class MessageReceivedEventArgs : System.EventArgs
    {
        public ClusterMessage Message { get; set; } = new();
        public IPEndPoint RemoteEndPoint { get; set; } = new(IPAddress.None, 0);
        public AcknowledgmentMessage? Response { get; set; }
    }
}