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
    options.Url = "http://localhost:9007";
    options.EventStore = "my-event-store";
});
```

## Features

The ASP.NET Core client provides specialized functionality for web applications:

- **Namespace Resolution**: Built-in support for HTTP header-based tenant resolution
- **HTTP Context Integration**: Automatic access to current HTTP request context
- **Scoped Services**: Event store instances are scoped to the HTTP request
- **Identity Provider**: Automatic identity extraction from claims or headers

## Topics

- [Namespace Resolution](namespace-resolution.md) - Configure how tenants and namespaces are resolved from HTTP requests
