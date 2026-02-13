# DotNET client usage

Chronicle uses connection strings to configure the .NET client. You can pass a connection string to `ChronicleOptions` or provide one when constructing a `ChronicleClient`.

## Development defaults

The `ChronicleConnectionString.Development` constant provides a pre-configured development connection string. Default constructors also use this value:

```csharp
var options = new ChronicleOptions();
var client = new ChronicleClient();
```

This is equivalent to:

```csharp
var options = ChronicleOptions.FromDevelopmentConnectionString();
var client = new ChronicleClient(ChronicleConnectionString.Development);
```

Use explicit configuration for production environments.

## From connection string

```csharp
var options = ChronicleOptions.FromConnectionString("chronicle://localhost:35000");
var client = new ChronicleClient(options);
```

## Fluent builder

Use `ChronicleConnectionStringBuilder` to construct a connection string programmatically:

```csharp
var connectionString = new ChronicleConnectionStringBuilder()
    .WithHost("server.example.com")
    .WithPort(35000)
    .WithCredentials("clientId", "clientSecret")
    .Build();

var options = ChronicleOptions.FromConnectionString(connectionString);
```

## Authentication validation

You cannot specify both client credentials and API key authentication in the same connection string. Doing so throws an `AmbiguousAuthenticationMode` error.

