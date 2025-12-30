# Connection Strings

Chronicle uses connection strings to configure how clients connect to the Chronicle server. Connection strings provide a flexible way to specify server addresses, authentication, TLS settings, and other connection parameters.

## Connection String Format

Chronicle connection strings follow this general format:

```text
chronicle://[username:password@]host[:port][/?options]
```

### Components

- **Scheme**: `chronicle://` or `chronicle+srv://` (for DNS SRV record lookup)
- **Authentication** (optional): `username:password@` for client credentials authentication
- **Host**: The server hostname or IP address
- **Port** (optional): The server port (defaults to 35000)
- **Options** (optional): Query string parameters for additional configuration

### Examples

```csharp
// Basic connection to localhost
var connectionString = "chronicle://localhost:35000";

// Connection with client credentials
var connectionString = "chronicle://myClientId:myClientSecret@server.example.com:35000";

// Connection with API key authentication
var connectionString = "chronicle://server.example.com:35000/?apiKey=your-api-key";

// Connection with TLS disabled (development only)
var connectionString = "chronicle://localhost:35000/?disableTls=true";
```

## Using Connection Strings

### From Configuration

The most common way to use connection strings is through configuration:

**appsettings.json:**

```json
{
  "Chronicle": {
    "Url": "chronicle://localhost:35000"
  }
}
```

**Program.cs:**

```csharp
builder.Services.AddChronicle();
```

The client will automatically read the connection string from the `Cratis:Chronicle:ConnectionString` configuration section.

### Programmatic Creation

You can also create connection strings programmatically:

```csharp
var options = new ChronicleOptions
{
    Url = "chronicle://localhost:35000"
};
```

Or use the factory method:

```csharp
var options = ChronicleOptions.FromConnectionString("chronicle://localhost:35000");
```

## Fluent API

Chronicle provides a fluent API through `ChronicleConnectionStringBuilder` for building connection strings programmatically:

```csharp
var builder = new ChronicleConnectionStringBuilder();

var connectionString = builder
    .WithHost("server.example.com")
    .WithPort(35000)
    .WithCredentials("myClientId", "myClientSecret")
    .Build();
```

### Available Methods

#### WithHost

Sets the server hostname or IP address:

```csharp
builder.WithHost("server.example.com")
```

#### WithPort

Sets the server port:

```csharp
builder.WithPort(35000)
```

#### WithCredentials

Configures client credentials authentication (OAuth 2.0 client credentials flow):

```csharp
builder.WithCredentials("clientId", "clientSecret")
```

#### WithApiKey

Configures API key authentication:

```csharp
builder.WithApiKey("your-api-key-here")
```

#### WithTlsDisabled

Disables TLS for the connection (useful for development environments):

```csharp
builder.WithTlsDisabled()
```

**Note:** Only disable TLS in development environments. Production systems should always use TLS.

#### WithTlsEnabled

Explicitly enables TLS (this is the default):

```csharp
builder.WithTlsEnabled()
```

#### WithScheme

Sets the connection scheme:

```csharp
builder.WithScheme("chronicle+srv") // For DNS SRV lookup
```

#### Build

Builds the connection string:

```csharp
string connectionString = builder.Build();
```

#### ToConnectionString

Converts the builder to a `ChronicleConnectionString` instance:

```csharp
ChronicleConnectionString connectionString = builder.ToConnectionString();
```

### Complete Example

```csharp
var connectionString = new ChronicleConnectionStringBuilder()
    .WithHost("chronicle.production.example.com")
    .WithPort(35000)
    .WithCredentials("production-client", "secure-secret")
    .Build();

var options = ChronicleOptions.FromConnectionString(connectionString);

// Use options to configure the client
```

## ChronicleConnectionString Methods

The `ChronicleConnectionString` class also provides fluent methods for creating modified connection strings:

```csharp
var baseConnection = new ChronicleConnectionString("chronicle://localhost:35000");

// Add credentials
var withCredentials = baseConnection.WithCredentials("clientId", "clientSecret");

// Add API key
var withApiKey = baseConnection.WithApiKey("api-key");
```

## Authentication

Chronicle supports multiple authentication modes:

### None

No authentication (default):

```csharp
var connectionString = "chronicle://localhost:35000";
```

### Client Credentials

OAuth 2.0 client credentials flow:

```csharp
// Via URL
var connectionString = "chronicle://clientId:clientSecret@server.example.com:35000";

// Via builder
var connectionString = new ChronicleConnectionStringBuilder()
    .WithHost("server.example.com")
    .WithCredentials("clientId", "clientSecret")
    .Build();
```

### API Key

API key authentication:

```csharp
// Via URL
var connectionString = "chronicle://server.example.com:35000/?apiKey=your-api-key";

// Via builder
var connectionString = new ChronicleConnectionStringBuilder()
    .WithHost("server.example.com")
    .WithApiKey("your-api-key")
    .Build();
```

### Authentication Validation

You cannot specify both client credentials and API key authentication in the same connection string. Attempting to do so will throw an `AmbiguousAuthenticationMode` exception:

```csharp
// This will throw AmbiguousAuthenticationMode
var connectionString = "chronicle://user:pass@server.example.com:35000/?apiKey=key";
```

The authentication mode is automatically determined based on what credentials are provided:

- If username and password are present → `ClientCredentials`
- If API key is present → `ApiKey`
- If neither are present → `None`

## Query Parameters

Connection strings support various query parameters:

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `apiKey` | string | API key for API key authentication | `?apiKey=your-api-key` |
| `disableTls` | boolean | Disables TLS (development only) | `?disableTls=true` |

### Example with Multiple Parameters

```csharp
var connectionString = "chronicle://server.example.com:35000/?apiKey=my-key&disableTls=false";
```

## TLS Configuration

By default, Chronicle uses TLS for secure communication. You can configure TLS settings through the connection string or `ChronicleOptions`:

### Disable TLS (Development Only)

**Connection String:**

```csharp
var connectionString = "chronicle://localhost:35000/?disableTls=true";
```

**Builder:**

```csharp
var connectionString = new ChronicleConnectionStringBuilder()
    .WithHost("localhost")
    .WithTlsDisabled()
    .Build();
```

**ChronicleOptions:**

```csharp
var options = ChronicleOptions.FromConnectionString("chronicle://localhost:35000/?disableTls=true");
// The Tls.Disable property will be set to true automatically
```

### Advanced TLS Configuration

For production environments, you can configure TLS certificates through `ChronicleOptions`:

```csharp
var options = new ChronicleOptions
{
    Url = "chronicle://server.example.com:35000",
    Tls = new Tls
    {
        CertificatePath = "/path/to/certificate.pfx",
        CertificatePassword = "certificate-password"
    }
};
```

See the [TLS Configuration](../../tls-configuration.md) documentation for more details on certificate setup.

## Best Practices

1. **Store connection strings in configuration**, not in code
2. **Use environment variables** for sensitive information like passwords and API keys
3. **Always use TLS in production** - only disable it for local development
4. **Use the fluent API** when building connection strings programmatically for better readability
5. **Validate connection strings** before deploying to production

## Environment-Specific Configuration

Use different connection strings for different environments:

**appsettings.Development.json:**

```json
{
  "Chronicle": {
    "Url": "chronicle://localhost:35000/?disableTls=true"
  }
}
```

**appsettings.Production.json:**

```json
{
  "Chronicle": {
    "Url": "chronicle://clientId:clientSecret@chronicle.production.example.com:35000"
  }
}
```

Or use environment variables:

```bash
export CHRONICLE__URL="chronicle://clientId:clientSecret@server.example.com:35000"
```

## Troubleshooting

### Connection Refused

Check that:

- The server is running
- The host and port are correct
- Firewall rules allow the connection

### Authentication Failures

Verify that:

- Credentials are correct
- The authentication mode matches the server configuration
- API keys are valid and not expired

### TLS Errors

If you encounter TLS errors:

- Ensure the server certificate is valid
- Check that the certificate is trusted
- Verify the certificate path and password are correct
- For development, consider using `disableTls=true` (not recommended for production)

## See Also

- [TLS Configuration](../../tls-configuration.md)
- [Namespaces](namespaces.md)
- [Getting Started](../../get-started/index.md)
