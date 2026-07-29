---
title: Registering External Services from code
description: Register External Services programmatically from the .NET client, as an alternative to configuring them in the Workbench.
---

[External Services](/chronicle/external-services/) are usually configured in the Workbench, but the .NET client also exposes a programmatic API on `IEventStore`:

```csharp
// An HTTP service with bearer token authentication and a header
await eventStore.ExternalServices.Register("CustomersApi", _ => _
    .Http("https://api.example.com")
    .WithBearerToken(token)
    .WithHeader("X-Tenant", "acme"));

// A PostgreSQL database service
await eventStore.ExternalServices.Register("CustomersDb", _ => _
    .PostgreSql("db.example.com", "customers", "postgres", password, port: 5432));
```

The builder also exposes `WithBasicAuth`, `WithOAuth`, `MsSql`, and `WithOption`.
