# External Services

External Services are named, per–event store configurations that describe **how to connect to something outside Chronicle** — an HTTP API or a database. Instead of scattering URLs, connection strings, and secrets across features (such as [captures](../captures/index.md)), you configure a connection once as an External Service and reference it by name.

## Why External Services

- **One place for connection details.** Base URLs, hosts, credentials, and provider options live on the service, not inside the feature that uses it.
- **Secrets stay out of declarations.** A [capture](../captures/index.md) API source references `api <Name>` — the URL and authentication come from the referenced External Service, so no secrets live in capture text.
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

## Using External Services from captures

A [capture](../captures/index.md) API source references an External Service by name. The capture only declares *what* to poll (the route and interval); *where* and *how to authenticate* come from the External Service:

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
