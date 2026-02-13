# Configuration

The .NET client can read its connection string from configuration. A typical setup uses `appsettings.json` with the `Chronicle:Url` value and the `AddChronicle()` registration.

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

## Environment-specific configuration

Use per-environment configuration files to separate development and production settings:

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

## Environment variables

You can also supply the connection string via environment variables:

```bash
export CHRONICLE__URL="chronicle://clientId:clientSecret@server.example.com:35000"
```

## Related topics

- [DotNET client usage](dotnet-client.md)
- [TLS configuration](../configuration/tls.md)

