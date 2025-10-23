# Zetian AOT Example

This example demonstrates how to use Zetian SMTP Server with Native AOT compilation (hybrid approach).

## Features Demonstrated

- ✅ Trimming-compatible code
- ✅ SMTP Server with AOT compilation
- ✅ Event handling without reflection
- ✅ Minimal reflection usage (only for JSON)
- ✅ Health Check service with reflection-based JSON serialization

## Hybrid AOT Approach

This example uses a hybrid AOT approach:
- **Core SMTP functionality**: Fully AOT-optimized
- **JSON serialization**: Uses reflection for flexibility (marked with attributes)

## Building

### Standard Build
```powershell
dotnet build
```

### AOT Publish (Windows x64)
```powershell
dotnet publish -c Release -r win-x64 --self-contained
```

### AOT Publish (Linux x64)
```powershell
dotnet publish -c Release -r linux-x64 --self-contained
```

### AOT Publish (macOS ARM64)
```powershell
dotnet publish -c Release -r osx-arm64 --self-contained
```

## Running

After publishing, run the native executable:

### Windows
```powershell
.\bin\Release\net10.0\win-x64\publish\Zetian.AotExample.exe
```

### Linux/macOS
```bash
./bin/Release/net10.0/linux-x64/publish/Zetian.AotExample
```

## Testing

1. **SMTP Server**: Connect to port 2525
   ```powershell
   telnet localhost 2525
   ```

2. **Health Check Endpoints**:
   ```powershell
   # General health
   curl http://localhost:8080/health
   
   # Liveness check
   curl http://localhost:8080/livez
   
   # Readiness check  
   curl http://localhost:8080/readyz
   ```

## Configuration

The example includes hybrid AOT optimizations:

- `StripSymbols=false`: Keep symbols for better debugging
- `IL2026;IL3050` warnings suppressed for JSON serialization
- `TrimMode=partial`: Partial trimming (preserves JSON types)
- `JsonSerializerIsReflectionEnabledByDefault=true`: JSON uses reflection

## Why Hybrid AOT?

The hybrid approach provides:
- **Flexibility**: Use anonymous types and dynamic JSON
- **Simplicity**: No need for source generators
- **Compatibility**: Works with existing JSON code
- **Performance**: Core SMTP functionality still AOT-optimized

## Notes

- Requires .NET 10.0 SDK or later
- macOS requires Xcode command line tools
- Linux requires clang and developer packages
- Visual Studio 2022 17.8+ recommended for Windows
- JSON serialization methods are marked with `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]` attributes