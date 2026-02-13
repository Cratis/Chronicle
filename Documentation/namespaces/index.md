# Namespaces

Namespaces provide logical segregation within a single event store. They allow Chronicle to keep events, projections, reducers, and read models isolated per namespace while still sharing the same physical infrastructure.

Use namespaces to:

- Separate tenant data in multi-tenant systems
- Partition environments like development, staging, and production
- Split business domains that share an event store

Namespaces are resolved for every operation. If no namespace is explicitly provided, the default namespace is used.

## Namespaces and tenancy

Namespaces are a practical way to implement tenant isolation. Each tenant can map to a namespace, ensuring that all event store operations remain scoped to that tenant. This keeps data separated without requiring separate event stores per tenant.

## Topics

- [DotNET client usage](dotnet-client.md)
- [ASP.NET Core namespace resolution](aspnetcore.md)

