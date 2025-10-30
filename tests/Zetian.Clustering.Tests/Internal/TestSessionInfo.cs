using System.Net;
using Zetian.Clustering.Abstractions;

namespace Zetian.Clustering.Tests.Internal
{
    internal class TestSessionInfo : ISessionInfo
    {
        public string SessionId { get; set; } = string.Empty;
        public IPAddress ClientIp { get; set; } = IPAddress.None;
        public int ClientPort { get; set; }
        public long EstimatedSize { get; set; }
        public int Priority { get; set; }
        public IReadOnlyDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}