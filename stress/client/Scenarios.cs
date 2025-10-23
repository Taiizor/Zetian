using MailKit.Net.Smtp;
using MimeKit;
using Prometheus;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Zetian.LoadGenerator;

public enum ScenarioType
{
    Throughput,
    Concurrent,
    Burst,
    Sustained
}

public interface ITestScenario
{
    string Name { get; }
    Task<TestResult> ExecuteAsync(ClientConfig config, CancellationToken cancellationToken);
}

public class TestResult
{
    public string ScenarioName { get; set; } = "";
    public TimeSpan Duration { get; set; }
    public long TotalMessages { get; set; }
    public long SuccessfulMessages { get; set; }
    public long FailedMessages { get; set; }
    public double MessagesPerSecond { get; set; }
    public double AverageLatency { get; set; }
    public double P95Latency { get; set; }
    public double P99Latency { get; set; }
    public List<string> Errors { get; set; } = new();
}

public static class ClientMetrics
{
    public static readonly Counter MessagesSent = Metrics
        .CreateCounter("client_messages_sent_total", "Total messages sent");

    public static readonly Counter MessagesFailed = Metrics
        .CreateCounter("client_messages_failed_total", "Total messages failed");

    public static readonly Histogram SendLatency = Metrics
        .CreateHistogram("client_send_latency_seconds", "Message send latency",
            new HistogramConfiguration
            {
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 15)
            });

    public static readonly Gauge ActiveConnections = Metrics
        .CreateGauge("client_active_connections", "Number of active connections");

    public static readonly Summary Throughput = Metrics
        .CreateSummary("client_throughput_messages_per_second", "Throughput in messages per second");
}

public abstract class BaseScenario : ITestScenario
{
    public abstract string Name { get; }

    protected MimeMessage CreateTestMessage(ClientConfig config, int index)
    {
        MimeMessage message = new();
        message.From.Add(new MailboxAddress($"Sender{index}", $"sender{index}@test.com"));

        for (int i = 0; i < config.Message.Recipients; i++)
        {
            message.To.Add(new MailboxAddress($"Recipient{i}", $"recipient{i}@test.com"));
        }

        message.Subject = $"Test Message {index} - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}";

        string bodyText = new('X', config.Message.Size);
        message.Body = new TextPart("plain") { Text = bodyText };

        return message;
    }

    protected async Task<(bool success, double latency)> SendMessageAsync(
        SmtpClient client,
        MimeMessage message,
        CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            await client.SendAsync(message, cancellationToken);
            stopwatch.Stop();

            double latency = stopwatch.Elapsed.TotalSeconds;
            ClientMetrics.MessagesSent.Inc();
            ClientMetrics.SendLatency.Observe(latency);

            return (true, latency);
        }
        catch (Exception)
        {
            stopwatch.Stop();
            ClientMetrics.MessagesFailed.Inc();
            return (false, stopwatch.Elapsed.TotalSeconds);
        }
    }

    public abstract Task<TestResult> ExecuteAsync(ClientConfig config, CancellationToken cancellationToken);
}

public class ThroughputScenario : BaseScenario
{
    public override string Name => "Throughput Test";

    public override async Task<TestResult> ExecuteAsync(ClientConfig config, CancellationToken cancellationToken)
    {
        TestResult result = new() { ScenarioName = Name };
        List<double> latencies = new();
        Stopwatch stopwatch = Stopwatch.StartNew();

        using SmtpClient client = new();
        await client.ConnectAsync(config.Target.Host, config.Target.Port, false, cancellationToken);
        ClientMetrics.ActiveConnections.Inc();

        try
        {
            int messageIndex = 0;
            TimeSpan targetInterval = TimeSpan.FromSeconds(1.0 / config.Scenario.Rate);
            TimeSpan nextSendTime = stopwatch.Elapsed;

            while (!cancellationToken.IsCancellationRequested && stopwatch.Elapsed < TimeSpan.FromSeconds(config.Scenario.Duration))
            {
                if (stopwatch.Elapsed >= nextSendTime)
                {
                    MimeMessage message = CreateTestMessage(config, messageIndex++);
                    (bool success, double latency) = await SendMessageAsync(client, message, cancellationToken);

                    latencies.Add(latency);

                    if (success)
                    {
                        result.SuccessfulMessages++;
                    }
                    else
                    {
                        result.FailedMessages++;
                    }

                    result.TotalMessages++;
                    nextSendTime += targetInterval;
                }

                await Task.Delay(1, cancellationToken);
            }
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
            ClientMetrics.ActiveConnections.Dec();
        }

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;
        result.MessagesPerSecond = result.TotalMessages / result.Duration.TotalSeconds;

        if (latencies.Any())
        {
            latencies.Sort();
            result.AverageLatency = latencies.Average();
            result.P95Latency = GetPercentile(latencies, 0.95);
            result.P99Latency = GetPercentile(latencies, 0.99);
        }

        ClientMetrics.Throughput.Observe(result.MessagesPerSecond);

        return result;
    }

    private double GetPercentile(List<double> sortedValues, double percentile)
    {
        int index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
        return sortedValues[Math.Max(0, Math.Min(index, sortedValues.Count - 1))];
    }
}

public class ConcurrentScenario : BaseScenario
{
    public override string Name => "Concurrent Connections Test";

    public override async Task<TestResult> ExecuteAsync(ClientConfig config, CancellationToken cancellationToken)
    {
        TestResult result = new() { ScenarioName = Name };
        Stopwatch stopwatch = Stopwatch.StartNew();
        ConcurrentBag<double> latencies = new();

        List<Task> tasks = new();
        SemaphoreSlim semaphore = new(config.Scenario.Connections);

        for (int i = 0; i < config.Scenario.Connections; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    await RunConnection(config, i, result, latencies, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;
        result.MessagesPerSecond = result.TotalMessages / result.Duration.TotalSeconds;

        List<double> latencyList = latencies.ToList();
        if (latencyList.Any())
        {
            latencyList.Sort();
            result.AverageLatency = latencyList.Average();
            result.P95Latency = GetPercentile(latencyList, 0.95);
            result.P99Latency = GetPercentile(latencyList, 0.99);
        }

        ClientMetrics.Throughput.Observe(result.MessagesPerSecond);

        return result;
    }

    private async Task RunConnection(
        ClientConfig config,
        int connectionId,
        TestResult result,
        System.Collections.Concurrent.ConcurrentBag<double> latencies,
        CancellationToken cancellationToken)
    {
        using SmtpClient client = new();
        await client.ConnectAsync(config.Target.Host, config.Target.Port, false, cancellationToken);
        ClientMetrics.ActiveConnections.Inc();

        try
        {
            DateTime endTime = DateTime.UtcNow.AddSeconds(config.Scenario.Duration);
            int messageIndex = 0;

            while (DateTime.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
            {
                MimeMessage message = CreateTestMessage(config, messageIndex++);
                (bool success, double latency) = await SendMessageAsync(client, message, cancellationToken);

                latencies.Add(latency);

                lock (result)
                {
                    if (success)
                    {
                        result.SuccessfulMessages++;
                    }
                    else
                    {
                        result.FailedMessages++;
                    }

                    result.TotalMessages++;
                }

                await Task.Delay(TimeSpan.FromSeconds(1.0 / config.Scenario.Rate), cancellationToken);
            }
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
            ClientMetrics.ActiveConnections.Dec();
        }
    }

    private double GetPercentile(List<double> sortedValues, double percentile)
    {
        int index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
        return sortedValues[Math.Max(0, Math.Min(index, sortedValues.Count - 1))];
    }
}

public class BurstScenario : BaseScenario
{
    public override string Name => "Burst Load Test";

    public override async Task<TestResult> ExecuteAsync(ClientConfig config, CancellationToken cancellationToken)
    {
        TestResult result = new() { ScenarioName = Name };
        List<double> latencies = new();
        Stopwatch stopwatch = Stopwatch.StartNew();

        using SmtpClient client = new();
        await client.ConnectAsync(config.Target.Host, config.Target.Port, false, cancellationToken);
        ClientMetrics.ActiveConnections.Inc();

        try
        {
            int burstSize = config.Scenario.Rate;
            TimeSpan burstInterval = TimeSpan.FromSeconds(5); // Burst every 5 seconds
            TimeSpan nextBurstTime = stopwatch.Elapsed;
            int messageIndex = 0;

            while (!cancellationToken.IsCancellationRequested && stopwatch.Elapsed < TimeSpan.FromSeconds(config.Scenario.Duration))
            {
                if (stopwatch.Elapsed >= nextBurstTime)
                {
                    // Send burst
                    List<Task<(bool, double)>> burstTasks = new();
                    for (int i = 0; i < burstSize; i++)
                    {
                        MimeMessage message = CreateTestMessage(config, messageIndex++);
                        burstTasks.Add(SendMessageAsync(client, message, cancellationToken));
                    }

                    (bool, double)[] results = await Task.WhenAll(burstTasks);

                    foreach ((bool success, double latency) in results)
                    {
                        latencies.Add(latency);

                        if (success)
                        {
                            result.SuccessfulMessages++;
                        }
                        else
                        {
                            result.FailedMessages++;
                        }

                        result.TotalMessages++;
                    }

                    nextBurstTime += burstInterval;
                }

                await Task.Delay(100, cancellationToken);
            }
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
            ClientMetrics.ActiveConnections.Dec();
        }

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;
        result.MessagesPerSecond = result.TotalMessages / result.Duration.TotalSeconds;

        if (latencies.Any())
        {
            latencies.Sort();
            result.AverageLatency = latencies.Average();
            result.P95Latency = GetPercentile(latencies, 0.95);
            result.P99Latency = GetPercentile(latencies, 0.99);
        }

        ClientMetrics.Throughput.Observe(result.MessagesPerSecond);

        return result;
    }

    private double GetPercentile(List<double> sortedValues, double percentile)
    {
        int index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
        return sortedValues[Math.Max(0, Math.Min(index, sortedValues.Count - 1))];
    }
}

public class SustainedScenario : BaseScenario
{
    public override string Name => "Sustained Load Test";

    public override async Task<TestResult> ExecuteAsync(ClientConfig config, CancellationToken cancellationToken)
    {
        // Use concurrent scenario with longer duration
        ConcurrentScenario concurrentScenario = new() { };
        return await concurrentScenario.ExecuteAsync(config, cancellationToken);
    }
}