---
title: Register Cratis Chronicle External Services in .NET
description: Register named HTTP or database connections in Cratis Chronicle from .NET, with authentication and headers kept outside capture declarations.
---

Cratis Chronicle [External Services](/chronicle/external-services/) are named HTTP or database connection settings for an event store. An API capture refers to an HTTP service by name. Register a service through `IEventStore.ExternalServices` when you want to configure the connection from .NET rather than enter it in the Workbench.

The HTTP example registers a base URL, bearer token, and tenant header under `CustomersApi`. The database example registers a PostgreSQL connection under `CustomersDb`. Supply `token` and `password` from your application's configuration; they are variables, not literal credentials in the example.

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

Use `WithBasicAuth` or `WithOAuth` instead of `WithBearerToken` when the HTTP service requires a different authorization scheme. The builder also exposes `MsSql` for Microsoft SQL Server connections and `WithOption` for additional connection options. Registering a database connection does not make it an API-polling source; API captures require an HTTP service.

For OAuth client credentials with a scope, use `WithOAuth(authority, clientId, clientSecret, scope)`. The three-argument overload requests no scope. For Microsoft Entra, the scope is typically `api://<application-id>/.default`. The kernel shares OAuth tokens and applies a current token to each HTTP request, so long-lived clients can refresh expired tokens without being recreated. A `401` response triggers one token refresh and retry.

Registration defines the connection; it does not create or start a capture. Next, use the registered name in a [capture source](captures/declarative.md). An API capture reads the base URL directly unless you configure a route.
