# Configuration

The .NET client reads its connection string from configuration. A typical setup binds the `Cratis:Chronicle` section of `appsettings.json` and registers Chronicle on the host with `AddCratisChronicle`.

**appsettings.json:**

```json
{
  "Cratis": {
    "Chronicle": {
      "ConnectionString": "chronicle://localhost:35000"
    }
  }
}
```

> [!NOTE]
> For local development, `chronicle://localhost:35000` can be used without credentials. The .NET client automatically attempts development credentials.

**Program.cs:**

```csharp
builder.AddCratisChronicle(options => options.EventStore = "my-store");
```

`AddCratisChronicle` is an extension on the host builder — `IHostApplicationBuilder` for worker and console hosts, `WebApplicationBuilder` for ASP.NET Core. It binds the `Cratis:Chronicle` section automatically, so the connection string comes from configuration rather than code. See [Add Chronicle to a worker service](../get-started/worker.md) and [Add Chronicle to an ASP.NET Core app](../get-started/aspnetcore.md) for the full host setup.

## Environment-specific configuration

Use per-environment configuration files to separate development and production settings:

**appsettings.Development.json:**

```json
{
  "Cratis": {
    "Chronicle": {
      "ConnectionString": "chronicle://localhost:35000/?disableTls=true"
    }
  }
}
```

**appsettings.Production.json:**

```json
{
  "Cratis": {
    "Chronicle": {
      "ConnectionString": "chronicle://clientId:clientSecret@chronicle.production.example.com:35000"
    }
  }
}
```

## Environment variables

You can also supply the connection string via environment variables. .NET maps the `__` separator onto nested configuration keys, so `Cratis:Chronicle:ConnectionString` becomes:

```bash
export Cratis__Chronicle__ConnectionString="chronicle://clientId:clientSecret@server.example.com:35000"
```

## Related topics

- [DotNET client usage](dotnet-client.md)
- [ChronicleOptions reference](../configuration/chronicle-options.md)
- [TLS configuration](../configuration/tls.md)
