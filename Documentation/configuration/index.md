# Configuration

Chronicle always runs as a **separate process** — the kernel — and your application talks to it as a **client** over gRPC. There is no in-process or embedded kernel to host inside your app, so on this side you're configuring *how your application connects and behaves as a client*. (To configure the kernel itself — ports, storage backends, features — see [Hosting Configuration](../hosting/configuration/index.md).)

## How client configuration flows

Every client setting can be supplied three ways, and they layer in this order — later wins:

1. **`appsettings.json`**, under the `Cratis:Chronicle` section — the usual home for the connection string and most settings.
2. **Environment variables**, with the `Cratis__Chronicle__` prefix (.NET maps the `__` separator onto nested keys) — convenient for containers and CI.
3. **Code**, via the `configureOptions` callback on `AddCratisChronicle` — it runs *after* binding, so it overrides configuration. Structural dependencies (custom resolvers and providers) can only be set here.

A minimal `appsettings.json`:

```json
{
  "Cratis": {
    "Chronicle": {
      "ConnectionString": "chronicle://localhost:35000",
      "EventStore": "my-store"
    }
  }
}
```

The matching registration on the host — `AddCratisChronicle` reads the section above, and the callback overrides it:

```csharp
builder.AddCratisChronicle(options => options.EventStore = "my-store");
```

`AddCratisChronicle` is an extension on the host builder — `IHostApplicationBuilder` for worker and console hosts, `WebApplicationBuilder` for ASP.NET Core. See the [getting-started host guides](../get-started/toc.yml) for the full setup of each.

## Topics

- [ChronicleOptions Reference](chronicle-options.md) - Complete reference for all `ChronicleOptions`, `ChronicleClientOptions`, and `ChronicleAspNetCoreOptions` settings, including `appsettings.json` binding.
- [Connection Strings](../connection-strings/index.md) - The `chronicle://` connection string format, development defaults, and credentials.
- [Namespaces](../namespaces/index.md) - Resolve the event store namespace per request — the basis of multi-tenancy.
- [Camel Casing](camel-casing.md) - Configure camel case naming policy for projection definitions and read model persistence.
- [gRPC Message Size](grpc-message-size.md) - Configure maximum gRPC message sizes for large event batches.
- [Structural Dependencies](structural-dependencies.md) - Configure artifact discovery, identity, namespace resolution, and other build-time dependencies via `ChronicleClient` constructor parameters or `IChronicleBuilder`.
- [TLS](tls.md) - Configure client-side TLS options and certificate handling.

## Configuring the server

The kernel is configured separately from the client. See [Hosting Configuration](../hosting/configuration/index.md) for the `chronicle.json` file, environment variables, storage backends, and ports. To stand the kernel up locally, see [Run Chronicle locally](../get-started/choose-hosting-model#run-chronicle-locally).
