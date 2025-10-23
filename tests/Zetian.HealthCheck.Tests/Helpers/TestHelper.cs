using System.Net;
using System.Net.Sockets;

namespace Zetian.HealthCheck.Tests.Helpers
{
    /// <summary>
    /// Common test helper utilities
    /// </summary>
    public static class TestHelper
    {
        /// <summary>
        /// Gets an available TCP port for testing
        /// </summary>
        /// <returns>An available port number</returns>
        public static int GetAvailablePort()
        {
            using TcpListener listener = new(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        /// <summary>
        /// Gets multiple available TCP ports for testing
        /// </summary>
        /// <param name="count">Number of ports needed</param>
        /// <returns>Array of available port numbers</returns>
        public static int[] GetAvailablePorts(int count)
        {
            int[] ports = new int[count];
            for (int i = 0; i < count; i++)
            {
                ports[i] = GetAvailablePort();
            }
            return ports;
        }
    }
}