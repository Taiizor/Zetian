# Zetian SMTP Server Benchmarks

Performance benchmarks comparing different Stream-based approaches and optimizations for SMTP operations.

## 📊 Benchmark Categories

### 1. SmtpSessionBenchmarks
- **ReadWithStreamReader**: Standard StreamReader approach (baseline)
- **ReadWithBufferedStream**: Using BufferedStream for enhanced I/O performance
- **ReadWithArrayPool**: Using ArrayPool for memory management
- **WriteWithStreamWriter**: Standard StreamWriter approach
- **WriteWithBufferedStream**: Using BufferedStream for write operations
- **WriteWithArrayPool**: Direct byte array operations with ArrayPool

### 2. AuthenticationBenchmarks
- **PlainAuth_Direct**: Direct authentication without I/O overhead (baseline)
- **PlainAuth_WithStreamReaderWriter**: PLAIN authentication with Stream I/O
- **LoginAuth_Direct**: Direct LOGIN authentication simulation
- **LoginAuth_WithStreamReaderWriter**: LOGIN authentication with Stream I/O

### 3. FilteringBenchmarks
- **ProtocolLevelFiltering**: Tests early rejection of messages based on domain rules
- **EventBasedFiltering**: Tests filtering after message reception
- **CompositeFiltering**: Tests chaining multiple filters together

### 4. StorageBenchmarks
- **FileStorageWithDateFolders**: Tests file storage with organized date-based directory structure
- **NullStorage**: Tests no-op storage for baseline comparison
- **InMemoryStorage**: Simulates in-memory message storage
- **FileStorageFlat**: Tests flat file storage without directory structure

### 5. MessageProcessingBenchmarks
- Tests with different message sizes (1KB, 10KB, 100KB)
- **WithMemoryStream**: Standard MemoryStream approach (baseline)
- **WithBufferedStream**: Using BufferedStream for improved buffering
- **WithArrayPool**: Using ArrayPool for efficient memory reuse

## 🚀 Running Benchmarks

### Prerequisites
- .NET 10.0 SDK or later
- Release build configuration for accurate results

### Command Line
```bash
cd benchmarks/Zetian.Benchmarks
dotnet run -c Release
```

### Run Specific Benchmark
```bash
dotnet run -c Release --filter *SmtpSessionBenchmarks*
dotnet run -c Release --filter *AuthenticationBenchmarks*
dotnet run -c Release --filter *FilteringBenchmarks*
dotnet run -c Release --filter *StorageBenchmarks*
dotnet run -c Release --filter *MessageProcessingBenchmarks*
```

## 📈 Expected Improvements

By using different optimization techniques, you should see:

- **BufferedStream**: Reduced I/O operations through buffering
- **ArrayPool**: Lower memory allocations and GC pressure
- **Direct byte operations**: Better performance for small operations
- **Stream optimizations**: Improved throughput for large messages

## 📊 Sample Results

```
| Method                      | Mean     | Error    | StdDev   | Allocated |
|---------------------------- |---------:|---------:|---------:|----------:|
| ReadWithStreamReader        | 125.3 μs |  2.43 μs |  2.28 μs |   24.5 KB |
| ReadWithBufferedStream      | 108.9 μs |  2.12 μs |  1.99 μs |   22.1 KB |
| ReadWithArrayPool           |  95.2 μs |  1.85 μs |  1.73 μs |   18.3 KB |
| ProtocolLevelFiltering      | 1.2 ms   |  0.05 ms | 0.04 ms  |  120.5 KB |
| EventBasedFiltering         | 0.8 ms   |  0.03 ms | 0.03 ms  |   45.2 KB |
| FileStorageWithDateFolders  | 4.5 ms   |  0.12 ms | 0.11 ms  |  250.7 KB |
| InMemoryStorage            | 0.2 ms   |  0.01 ms | 0.01 ms  |   15.8 KB |
| ProcessMessage_MemoryStream | 458.2 μs |  8.92 μs |  8.34 μs |   87.3 KB |
| ProcessMessage_BufferedStream| 402.5 μs |  7.84 μs |  7.33 μs |   76.2 KB |
| ProcessMessage_ArrayPool    | 371.3 μs |  7.23 μs |  6.77 μs |   65.4 KB |
```

## 🔧 Configuration

The benchmarks are configured with:
- **MemoryDiagnoser**: Tracks memory allocations
- **SimpleJob**: Warm-up and iteration counts for consistent results
- **Multiple runs**: Ensures statistical significance

## 📝 Notes

- Results may vary based on hardware and system load
- Run benchmarks in Release mode for accurate measurements
- Close other applications to minimize interference
- Consider running multiple times for consistency