# ASP.NET Core Client

The Chronicle ASP.NET Core client provides seamless integration with ASP.NET Core applications, offering built-in support for dependency injection, HTTP context access, and web-specific features.

## Overview

The ASP.NET Core client extends the base .NET client with features specifically designed for web applications:

- HTTP header-based namespace resolution for multi-tenant scenarios
- Integration with ASP.NET Core's dependency injection container
- Automatic identity resolution from HTTP context
- Scoped event store instances per request

## Getting Started

Add Chronicle to your ASP.NET Core application:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddCratisChronicle(options =>
{
    options.EventStore = "my-event-store";
});
```

### Configuration from appsettings.json

You can configure Chronicle using your `appsettings.json` file. The configuration system will automatically bind the settings to `ChronicleOptions`:

```json
{
  "Chronicle": {
    "Url": "chronicle://localhost:35000",
    "SoftwareVersion": "1.0.0",
    "SoftwareCommit": "abc123",
    "ProgramIdentifier": "my-service",
    "AutoDiscoverAndRegister": true,
    "ConnectTimeout": 5,
    "MaxReceiveMessageSize": 104857600,
    "MaxSendMessageSize": 104857600
  }
}
```

Then configure Chronicle to use the configuration:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ChronicleOptions>(
    builder.Configuration.GetSection("Chronicle"));

builder.AddCratisChronicle();
```

### Configuration from Environment Variables

Chronicle configuration can also be set using environment variables, which is useful for containerized deployments:

```bash
# Chronicle server URL
Chronicle__Url=chronicle://my-server:35000

# Software metadata
Chronicle__SoftwareVersion=1.0.0
Chronicle__SoftwareCommit=abc123
Chronicle__ProgramIdentifier=my-service

# Connection settings
Chronicle__ConnectTimeout=10
Chronicle__AutoDiscoverAndRegister=true

# gRPC message size limits (in bytes, defaults to 100 MB)
Chronicle__MaxReceiveMessageSize=104857600
Chronicle__MaxSendMessageSize=104857600
```

Environment variables take precedence over `appsettings.json`, making them ideal for environment-specific configuration.

## Features

The ASP.NET Core client provides specialized functionality for web applications:

- **Namespace Resolution**: Built-in support for HTTP header-based tenant resolution
- **HTTP Context Integration**: Automatic access to current HTTP request context
- **Scoped Services**: Event store instances are scoped to the HTTP request
- **Identity Provider**: Automatic identity extraction from claims or headers

## Topics

- [Namespace Resolution](namespace-resolution.md) - Configure how tenants and namespaces are resolved from HTTP requests
