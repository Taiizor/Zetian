using Microsoft.AspNetCore.Builder;
using System;
using System.Threading;
using System.Threading.Tasks;
using Zetian.WebUI.Options;

namespace Zetian.WebUI.Services
{
    /// <summary>
    /// Interface for the WebUI service
    /// </summary>
    public interface IWebUIService : IDisposable
    {
        /// <summary>
        /// Gets the WebUI options
        /// </summary>
        WebUIOptions Options { get; }

        /// <summary>
        /// Gets whether the WebUI is running
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Gets the WebUI URL
        /// </summary>
        string Url { get; }

        /// <summary>
        /// Starts the WebUI service
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the WebUI service
        /// </summary>
        Task StopAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Configures the web application
        /// </summary>
        void ConfigureApp(Action<WebApplication> configure);

        /// <summary>
        /// Occurs when a client connects to the WebUI
        /// </summary>
        event EventHandler<ClientEventArgs>? OnClientConnected;

        /// <summary>
        /// Occurs when a client disconnects from the WebUI
        /// </summary>
        event EventHandler<ClientEventArgs>? OnClientDisconnected;

        /// <summary>
        /// Occurs when an API request is made
        /// </summary>
        event EventHandler<ApiRequestEventArgs>? OnApiRequest;
    }

    /// <summary>
    /// Event arguments for client events
    /// </summary>
    public class ClientEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the client ID
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// Gets the client IP address
        /// </summary>
        public string IpAddress { get; }

        /// <summary>
        /// Gets the connection time
        /// </summary>
        public DateTime ConnectionTime { get; }

        /// <summary>
        /// Initializes a new instance of ClientEventArgs
        /// </summary>
        public ClientEventArgs(string clientId, string ipAddress)
        {
            ClientId = clientId;
            IpAddress = ipAddress;
            ConnectionTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event arguments for API requests
    /// </summary>
    public class ApiRequestEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the request method
        /// </summary>
        public string Method { get; }

        /// <summary>
        /// Gets the request path
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the client IP address
        /// </summary>
        public string IpAddress { get; }

        /// <summary>
        /// Gets the response status code
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// Gets the request duration
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Initializes a new instance of ApiRequestEventArgs
        /// </summary>
        public ApiRequestEventArgs(string method, string path, string ipAddress, int statusCode, TimeSpan duration)
        {
            Method = method;
            Path = path;
            IpAddress = ipAddress;
            StatusCode = statusCode;
            Duration = duration;
        }
    }
}