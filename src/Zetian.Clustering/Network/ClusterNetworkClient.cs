using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Clustering.Abstractions;
using Zetian.Clustering.Models.EventArgs;

namespace Zetian.Clustering.Network
{
    /// <summary>
    /// Default implementation of cluster network client using TCP
    /// </summary>
    public class ClusterNetworkClient : IClusterNetworkClient, IDisposable
    {
        private readonly ILogger<ClusterNetworkClient> _logger;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<AcknowledgmentMessage>> _pendingRequests;
        private TcpListener? _listener;
        private CancellationTokenSource? _listenerCts;
        private Task? _listenerTask;
        private readonly SemaphoreSlim _sendLock;
        private readonly JsonSerializerOptions _jsonOptions;

        public ClusterNetworkClient(ILogger<ClusterNetworkClient> logger)
        {
            _logger = logger;
            _pendingRequests = new ConcurrentDictionary<string, TaskCompletionSource<AcknowledgmentMessage>>();
            _sendLock = new SemaphoreSlim(1, 1);
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };
        }

        public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

        public async Task<AcknowledgmentMessage?> SendMessageAsync(IPEndPoint endpoint, ClusterMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                using TcpClient client = new();
                await client.ConnectAsync(endpoint.Address, endpoint.Port, cancellationToken);

                using NetworkStream stream = client.GetStream();

                // Serialize and send message
                string json = JsonSerializer.Serialize(message, _jsonOptions);
                byte[] data = Encoding.UTF8.GetBytes(json);
                byte[] lengthBytes = BitConverter.GetBytes(data.Length);

                await stream.WriteAsync(lengthBytes, cancellationToken);
                await stream.WriteAsync(data, cancellationToken);
                await stream.FlushAsync(cancellationToken);

                // Wait for acknowledgment if required
                if (message.RequiresAck)
                {
                    TaskCompletionSource<AcknowledgmentMessage> tcs = new();
                    _pendingRequests[message.MessageId] = tcs;

                    // Set timeout
                    using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(message.Ttl);

                    try
                    {
                        // Read response
                        byte[] responseLengthBytes = new byte[4];
                        await stream.ReadAsync(responseLengthBytes, timeoutCts.Token);
                        int responseLength = BitConverter.ToInt32(responseLengthBytes, 0);

                        byte[] responseData = new byte[responseLength];
                        int totalRead = 0;
                        while (totalRead < responseLength)
                        {
                            int read = await stream.ReadAsync(responseData.AsMemory(totalRead, responseLength - totalRead), timeoutCts.Token);
                            if (read == 0)
                            {
                                break;
                            }

                            totalRead += read;
                        }

                        string responseJson = Encoding.UTF8.GetString(responseData, 0, totalRead);
                        AcknowledgmentMessage? ack = JsonSerializer.Deserialize<AcknowledgmentMessage>(responseJson, _jsonOptions);

                        return ack;
                    }
                    finally
                    {
                        _pendingRequests.TryRemove(message.MessageId, out _);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message to {Endpoint}", endpoint);
                return null;
            }
        }

        public async Task BroadcastMessageAsync(ClusterMessage message, CancellationToken cancellationToken = default)
        {
            // This would be implemented with multicast or by maintaining a list of known nodes
            // For now, this is a placeholder
            await Task.CompletedTask;
            _logger.LogDebug("Broadcasting message {MessageId} of type {Type}", message.MessageId, message.Type);
        }

        public async Task<bool> SendDataAsync(IPEndPoint endpoint, byte[] data, CancellationToken cancellationToken = default)
        {
            try
            {
                using TcpClient client = new();
                await client.ConnectAsync(endpoint.Address, endpoint.Port, cancellationToken);

                using NetworkStream stream = client.GetStream();
                await stream.WriteAsync(data, cancellationToken);
                await stream.FlushAsync(cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send data to {Endpoint}", endpoint);
                return false;
            }
        }

        public async Task StartListeningAsync(int port, CancellationToken cancellationToken = default)
        {
            if (_listener != null)
            {
                throw new InvalidOperationException("Already listening");
            }

            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();

            _listenerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _listenerTask = Task.Run(() => ListenAsync(_listenerCts.Token));

            await Task.CompletedTask;
            _logger.LogInformation("Started listening on port {Port}", port);
        }

        public async Task StopListeningAsync(CancellationToken cancellationToken = default)
        {
            _listenerCts?.Cancel();
            _listener?.Stop();

            if (_listenerTask != null)
            {
                await _listenerTask;
            }

            _listener = null;
            _listenerCts?.Dispose();
            _listenerCts = null;
            _listenerTask = null;

            _logger.LogInformation("Stopped listening");
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _listener != null)
            {
                try
                {
                    TcpClient client = await AcceptClientAsync(cancellationToken);
                    if (client != null)
                    {
                        _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error accepting client connection");
                }
            }
        }

        private async Task<TcpClient> AcceptClientAsync(CancellationToken cancellationToken)
        {
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            Task<TcpClient> tcpClientTask = _listener!.AcceptTcpClientAsync();
            Task delayTask = Task.Delay(Timeout.Infinite, cts.Token);

            Task completedTask = await Task.WhenAny(tcpClientTask, delayTask);

            if (completedTask == tcpClientTask)
            {
                return await tcpClientTask;
            }

            throw new OperationCanceledException();
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            try
            {
                using (client)
                {
                    using NetworkStream stream = client.GetStream();

                    // Read message length
                    byte[] lengthBytes = new byte[4];
                    await stream.ReadAsync(lengthBytes, cancellationToken);
                    int messageLength = BitConverter.ToInt32(lengthBytes, 0);

                    // Read message data
                    byte[] messageData = new byte[messageLength];
                    int totalRead = 0;
                    while (totalRead < messageLength)
                    {
                        int read = await stream.ReadAsync(messageData.AsMemory(totalRead, messageLength - totalRead), cancellationToken);
                        if (read == 0)
                        {
                            break;
                        }

                        totalRead += read;
                    }

                    // Deserialize message
                    string json = Encoding.UTF8.GetString(messageData, 0, totalRead);
                    ClusterMessage? message = JsonSerializer.Deserialize<ClusterMessage>(json, _jsonOptions);

                    if (message != null && client.Client.RemoteEndPoint is IPEndPoint remoteEndPoint)
                    {
                        // Raise event
                        MessageReceivedEventArgs args = new()
                        {
                            Message = message,
                            RemoteEndPoint = remoteEndPoint
                        };

                        MessageReceived?.Invoke(this, args);

                        // Send acknowledgment if required
                        if (message.RequiresAck && args.Response != null)
                        {
                            string ackJson = JsonSerializer.Serialize(args.Response, _jsonOptions);
                            byte[] ackData = Encoding.UTF8.GetBytes(ackJson);
                            byte[] ackLengthBytes = BitConverter.GetBytes(ackData.Length);

                            await stream.WriteAsync(ackLengthBytes, cancellationToken);
                            await stream.WriteAsync(ackData, cancellationToken);
                            await stream.FlushAsync(cancellationToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling client connection");
            }
        }

        public void Dispose()
        {
            _listenerCts?.Cancel();
            _listener?.Stop();
            _listenerTask?.Wait(TimeSpan.FromSeconds(5));
            _listenerCts?.Dispose();
            _sendLock?.Dispose();
        }
    }
}