# ChronicleOptions Reference

`ChronicleOptions` is the root configuration class for all Chronicle .NET clients. All properties can be set in code via the `configureOptions` callback on `AddCratisChronicle`, and most can be bound directly from `appsettings.json` under the `Cratis:Chronicle` section.

There are three options classes that form an inheritance chain, each adding settings relevant to its hosting context:

| Class | Used by | Adds |
|---|---|---|
| `ChronicleOptions` | All clients | Connection, serialization, timeouts, TLS, auth |
| `ChronicleClientOptions` | Worker services, console apps | Event store name, namespace resolver type |
| `ChronicleAspNetCoreOptions` | ASP.NET Core apps | HTTP header–based namespace resolution |

## appsettings.json

All bindable settings live under `Cratis:Chronicle`:

```json
{
  "Cratis": {
    "Chronicle": {
      "ConnectionString": "chronicle://localhost:35000",
      "EventStore": "my-store",
      "EnableEventTypeGenerationValidation": false,
      "DefaultSinkTypeId": "f7d3a1e2-4b5c-4d6e-8f9a-0b1c2d3e4f5a",
      "ConnectTimeout": 5,
      "AutoDiscoverAndRegister": true,
      "MaxReceiveMessageSize": 104857600,
      "MaxSendMessageSize": 104857600,
      "ManagementPort": 8080,
      "ProgramIdentifier": "my-app",
      "ClaimsBasedNamespaceResolverClaimType": "tenant_id",
      "Tls": {
        "CertificatePath": "/certs/client.pfx",
        "CertificatePassword": "secret"
      },
      "Authentication": {
        "Authority": "https://login.example.com"
      }
    }
  }
}
```

## ChronicleOptions properties

### EnableEventTypeGenerationValidation

Asks the Kernel to enforce strict migration chain validation when registering event types at generation 2 or higher. The value is forwarded as part of the registration request.

| | |
|---|---|
| Type | `bool` |
| Default | `false` |

> **Image restriction:** This flag is only honoured by the **development image** of the Kernel. The production image always ignores it and validates unconditionally, regardless of what the client sends. This makes it impossible to accidentally skip migration chain validation in production even if the client has this flag set to `false`.

To opt in to strict validation during development (recommended once your event schemas are stable), set the flag to `true`:

```csharp
builder.AddCratisChronicle(configureOptions: options =>
{
    options.EnableEventTypeGenerationValidation = true;
});
```

In `appsettings.json`: `"EnableEventTypeGenerationValidation": true`.

See [Generation Validation](../migrations/validation.md) for the full ruleset.

### ConnectionString

The gRPC endpoint for Chronicle Server.

| | |
|---|---|
| Type | `ChronicleConnectionString` |
| Default | Development connection string (`chronicle://localhost:35000` with default dev credentials) |

```csharp
options.ConnectionString = "chronicle://myserver:35000";
```

In `appsettings.json`: `"ConnectionString": "chronicle://myserver:35000"`.

### ConnectTimeout

Timeout in seconds when establishing a connection to Chronicle Server.

| | |
|---|---|
| Type | `int` |
| Default | `5` |

### AutoDiscoverAndRegister

Controls whether Chronicle automatically discovers event types, projections, reactors, reducers, and other artifacts from loaded assemblies on startup.

| | |
|---|---|
| Type | `bool` |
| Default | `true` |

Set to `false` when using a custom `IClientArtifactsProvider` or when you want full manual control over what gets registered.

### DefaultSinkTypeId

Controls which sink type is used when registering projections and reducers with the Kernel. Chronicle supports two built-in sink types:

| Value | Description |
|---|---|
| `WellKnownSinkTypes.MongoDB` | Stores read models in MongoDB (default) |
| `WellKnownSinkTypes.SQL` | Stores read models in a SQL database as JSON documents |

| | |
|---|---|
| Type | `SinkTypeId` |
| Default | `WellKnownSinkTypes.MongoDB` |

To use SQL as the storage backend for all read models:

```csharp
builder.AddCratisChronicle(configureOptions: options =>
{
    options.DefaultSinkTypeId = WellKnownSinkTypes.SQL;
});
```

In `appsettings.json`:

```json
{ "Cratis": { "Chronicle": { "DefaultSinkTypeId": "f7d3a1e2-4b5c-4d6e-8f9a-0b1c2d3e4f5a" } } }
```

See [Sinks](../sinks/index.md) for detailed guidance on storage backends.

### MaxReceiveMessageSize / MaxSendMessageSize

Maximum gRPC message sizes in bytes. See [gRPC Message Size](grpc-message-size.md) for detailed guidance.

| | |
|---|---|
| Type | `int?` |
| Default | `104857600` (100 MB) |

### ManagementPort

Port used for the Management API and the well-known certificate endpoint.

| | |
|---|---|
| Type | `int` |
| Default | `8080` |

### ProgramIdentifier

A human-readable identifier for the running application. Included in root causation metadata to identify which program appended an event.

| | |
|---|---|
| Type | `string` |
| Default | `"[N/A]"` |

### SoftwareVersion

The software version of the entry assembly, extracted from `AssemblyInformationalVersion` or `AssemblyVersion`. Included in root causation to track which build produced an event.

| | |
|---|---|
| Type | `string` |
| Default | Resolved from the entry assembly at startup |

### SoftwareCommit

The commit SHA of the entry assembly, extracted from the `+commit` part of `AssemblyInformationalVersion`. Included in root causation to track which commit produced an event.

| | |
|---|---|
| Type | `string` |
| Default | Resolved from the entry assembly at startup |

### ClaimsBasedNamespaceResolverClaimType

The JWT claim type used by `ClaimsBasedNamespaceResolver` when namespace resolution is configured via `WithClaimsBasedNamespaceResolver`.

| | |
|---|---|
| Type | `string` |
| Default | `"tenant_id"` |

### Tls

TLS certificate settings for the gRPC connection. TLS is active when both `CertificatePath` and `CertificatePassword` are set. See [TLS](tls.md) for full details.

| | |
|---|---|
| Type | `Tls` |
| Default | TLS disabled (no certificate configured) |

| Nested property | Type | Description |
|---|---|---|
| `CertificatePath` | `string?` | Path to a PFX certificate file |
| `CertificatePassword` | `string?` | Password for the certificate file |

### Authentication

OAuth/OIDC authority configuration. When `Authority` is not set, the Chronicle internal OAuth authority is used.

| | |
|---|---|
| Type | `Authentication` |
| Default | Internal authority |

| Nested property | Type | Description |
|---|---|---|
| `Authority` | `string?` | OAuth/OIDC authority URL |

## ChronicleClientOptions properties

`ChronicleClientOptions` extends `ChronicleOptions` and is used by worker services, console apps, and other non-web hosts via `IHostApplicationBuilder.AddCratisChronicle`.

### EventStore

The name of the event store the client connects to. Required.

| | |
|---|---|
| Type | `EventStoreName` |
| Default | *(none — must be set)* |

```json
{ "Cratis": { "Chronicle": { "EventStore": "my-store" } } }
```

### EventStoreNamespaceResolverType

The `Type` of the `IEventStoreNamespaceResolver` to use for resolving the event store namespace. When `null`, defaults to `DefaultEventStoreNamespaceResolver` at runtime, which always returns the default namespace.

| | |
|---|---|
| Type | `Type?` |
| Default | `null` → `DefaultEventStoreNamespaceResolver` |

This is a structural dependency — it resolves a type reference and cannot be meaningfully set from `appsettings.json`. Set it in code via the `configureOptions` callback or via `IChronicleBuilder`.

## ChronicleAspNetCoreOptions properties

`ChronicleAspNetCoreOptions` extends `ChronicleClientOptions` and is used by ASP.NET Core applications via `WebApplicationBuilder.AddCratisChronicle`. It defaults `EventStoreNamespaceResolverType` to `HttpHeaderEventStoreNamespaceResolver`.

### NamespaceHttpHeader

The HTTP request header used to resolve the event store namespace on a per-request basis.

| | |
|---|---|
| Type | `string` |
| Default | `"x-cratis-tenant-id"` |

```json
{ "Cratis": { "Chronicle": { "NamespaceHttpHeader": "x-my-tenant" } } }
```
