BenchmarkDotNet v0.15.4, Windows 11 (10.0.26200.6899)
11th Gen Intel Core i7-11800H 2.30GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.100-rc.2.25502.107
  [Host]     : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v4
  Job-TOWPHV : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v4

IterationCount=3  LaunchCount=1  WarmupCount=2

| Method                       | Mean    | Error    | StdDev   | Ratio | Rank | Allocated | Alloc Ratio |
|----------------------------- |--------:|---------:|---------:|------:|-----:|----------:|------------:|
| Zetian_SendSingleMessage     | 2.039 s | 0.1236 s | 0.0068 s |  1.00 |    1 |  69.57 KB |        1.00 |
| SmtpServer_SendSingleMessage | 2.044 s | 0.0477 s | 0.0026 s |  1.00 |    1 |  67.89 KB |        0.98 |
| Zetian_Send3Messages         | 6.097 s | 0.2220 s | 0.0122 s |  2.99 |    2 | 208.38 KB |        3.00 |
| SmtpServer_Send3Messages     | 6.104 s | 0.2636 s | 0.0144 s |  2.99 |    2 | 195.84 KB |        2.81 |