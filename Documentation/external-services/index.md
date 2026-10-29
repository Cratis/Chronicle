---
description: Chronicle external services are named, per-event-store configurations describing how to connect to an HTTP API or database outside Chronicle.
---
# External Services

External Services are named, per–event store configurations that describe **how to connect to something outside Chronicle** — an HTTP API or a database. Instead of scattering URLs, connection strings, and secrets across features (such as [captures](../captures/index.mdx)), you configure a connection once as an External Service and reference it by name.

## Why External Services

- **One place for connection details.** Base URLs, hosts, credentials, and provider options live on the service, not inside the feature that uses it.
- **Secrets stay out of declarations.** A [capture](../captures/index.mdx) API source references `api <Name>` — the URL and authentication come from the referenced External Service, so no secrets live in capture text.
- **Extensible.** The endpoint model is open — new endpoint types can be added without changing existing ones.

## Endpoint types

Every External Service has one endpoint of a specific type:

| Type | What it configures | Forms |
| --- | --- | --- |
| **HTTP** | Base URL, authorization (None / Basic / Bearer / OAuth), and headers | An HTTP client target |
| **MSSQL** | Host, port, database, username, password, and options | A Microsoft SQL Server connection string |
| **PostgreSQL** | Host, port, database, username, password, and options | A PostgreSQL connection string |

Database endpoints know how to **form their own connection string** from their configuration. Each database provider has its own formatter (`IFormDatabaseConnectionString`), discovered by convention — adding a new provider is a matter of implementing that interface for a new endpoint type.

### Connection string formation

For a database endpoint, the connection string is built from the configured fields:

- **MSSQL** → `Server=<host>[,<port>];Database=<db>;User Id=<user>;Password=<pw>;<options...>`
- **PostgreSQL** → `Host=<host>;[Port=<port>;]Database=<db>;Username=<user>;Password=<pw>;<options...>`

When the port is left unspecified (`0`), the provider default is used and the port is omitted from the connection string.

## Configuring External Services

External Services are configured per event store. In the **Workbench**, open an event store and go to **General → External Services** to add, view, and remove services. Choose the endpoint type and fill in the type-specific form:

- **HTTP** — base URL, an authorization type, the matching credential fields, and optional headers.
- **MSSQL / PostgreSQL** — host, port, database, username, and password.

Secrets (passwords, tokens, client secrets) are write-only from the Workbench — they are never returned by the read model that lists services.

External Services can also be registered programmatically — see [Registering External Services from code](/chronicle/clients/dotnet/external-services/) in the .NET client docs.

## HTTP capture destination policy

HTTP capture requests use the Kernel's `ExternalServices` destination policy, independently of
`Webhooks`. Approving a webhook host does not approve a capture source. Configure the section at the
top level of `chronicle.json`:

```json
{
  "externalServices": {
    "validation": "BlockLocalAddresses",
    "allowedHosts": ["api.example.com"],
    "eventStoreAllowedHosts": {
      "orders": ["orders-api.internal"]
    }
  }
}
```

`Validation` defaults to `Warn` in Development builds and `BlockLocalAddresses` otherwise. The local
mode blocks loopback (including mapped IPv4), IPv4 link-local `169.254/16`, IPv4 unspecified `0/8`,
IPv6 link-local `fe80::/10`, and IPv6 unspecified `::`. It warns about HTTP, missing host approval,
and other private/special destinations. `BlockPrivateAddresses` also blocks other private/special
addresses. `Enforce` blocks every violation; it remains reserved as the next-major default.
`Warn` permits violations with diagnostics, except cloud metadata addresses, which require explicit
host approval in every mode. Never approve metadata hosts in normal deployments.

`AllowedHosts` supports exact hosts and `*.example.com` subdomains, not the apex domain. An
`EventStoreAllowedHosts` entry replaces the global list, including an empty entry. Explicit approval
permits that host's private addresses. `AllowInsecureHttp` defaults to `false` and only applies in the
Development runtime environment. The corresponding environment variables use
`Cratis__Chronicle__ExternalServices__`, for example `Cratis__Chronicle__ExternalServices__Validation`.

Every capture request carries its event store. Absolute routes are validated against their final
host just like relative routes. Redirects and system proxies are disabled. DNS is validated when
opening each connection. Requests using explicit host allowances close their connections after each
request so one store's approval cannot be reused by another, even when an approved host currently
resolves publicly. Unapproved public destinations retain pooling in modes that permit them. This policy covers HTTP
capture polling, not database endpoints or OAuth token acquisition by external-service authentication.

Before upgrading a deployment with local capture APIs, explicitly approve the trusted hosts or select
`Warn` as a migration setting. Review other private-network hosts before choosing
`BlockPrivateAddresses` or `Enforce`. See [Kernel options](../configuration/chronicle-options.mdx#externalservices-kernel).

## Using External Services from captures

A [capture](../captures/index.mdx) API source references an External Service by name. The capture only declares *what* to poll (the route and interval); *where* and *how to authenticate* come from the External Service:

```cdl
capture Customers
  source api
    api CustomersApi   # references the "CustomersApi" External Service
    route /customers
    poll 5m
  key customerId
  ...
```

See [Capture Declaration Language](../captures/capture-declaration-language/index.md) for the full capture syntax.
