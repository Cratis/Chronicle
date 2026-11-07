---
title: Cratis Chronicle external-service connections
description: Cratis Chronicle External Services store named HTTP and database connections per event store, keeping connection details out of capture declarations.
---

<a id="external-services"></a>

An External Service in Cratis Chronicle is a named HTTP or database connection owned by an event store. Configure one before defining an API [capture](../captures/index.mdx) so the capture can refer to a name instead of carrying the service's URL and authentication.

## Why External Services

When several captures read the same API, they can reference one External Service. The service holds the base URL, credentials, and headers; each capture chooses its route and polling interval. Register that connection as `CustomersApi`, for example, then reference it with `api CustomersApi` without copying its authentication credentials into each declaration.

## Endpoint types

Choose the endpoint type for the system you want to connect to. Each External Service has one endpoint:

| Type | Connection settings | Used to create |
| --- | --- | --- |
| HTTP | Base URL, authorization (None / Basic / Bearer / OAuth), and headers | An HTTP client target |
| MSSQL | Host, port, database, username, password, and options | A Microsoft SQL Server connection string |
| PostgreSQL | Host, port, database, username, password, and options | A PostgreSQL connection string |

API captures require an HTTP endpoint. Registering a database connection does not make it an API-polling source. For database endpoints, Chronicle builds the connection string through a provider-specific `IFormDatabaseConnectionString` formatter, discovered by convention.

### Connection string formation

For a database endpoint, the connection string is built from the configured fields:

- **MSSQL** → `Server=<host>[,<port>];Database=<db>;User Id=<user>;Password=<pw>;<options...>`
- **PostgreSQL** → `Host=<host>;[Port=<port>;]Database=<db>;Username=<user>;Password=<pw>;<options...>`

When the port is left unspecified (`0`), the provider default is used and the port is omitted from the connection string.

## Configuring External Services

In the Workbench, open the event store that will use the connection and go to **General → External Services**. Add a service, give it a name, and choose its endpoint type.

For HTTP, enter the base URL and choose an authorization type from the table below. For MSSQL or PostgreSQL, enter the host, database, and any required username and password. The port is optional. After adding the service, confirm its name and endpoint in the service list; you can also view details or remove a selected service there.

Passwords, bearer tokens, and OAuth client secrets are write-only in the Workbench: the read model used to list services does not return those credential fields. Configured headers and database provider options are included in that read model.

To register connections from application code, follow [Registering External Services from code](/chronicle/clients/dotnet/external-services/) in the .NET client docs. Use that registration API for additional HTTP headers or database provider options; the Workbench form does not expose those fields.

### HTTP authentication

Choose the authorization type expected by the API:

| Authorization type | What to supply | How Chronicle authenticates |
| --- | --- | --- |
| None | No authorization credentials | No authorization header is added by this setting |
| Basic | Username and password | Sends an HTTP Basic authorization header |
| Bearer | Access token | Sends the supplied token in a Bearer authorization header |
| OAuth | Authority, client ID, client secret, and optional scope | Discovers the token endpoint from the authority and requests a token using the client-credentials grant |

OAuth uses the application's credentials, without an interactive user sign-in. Its discovery and token requests follow the HTTP capture destination policy below, so include the identity and token hosts when configuring host approvals.

## HTTP capture destination policy

Before polling an HTTP service, configure which destinations the Kernel may call. HTTP captures use the `ExternalServices` destination policy, independently of `Webhooks`: approving a webhook host does not approve a capture source or its OAuth hosts.

Put the section at the top level of `chronicle.json`:

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

`Validation` defaults to `Warn` in Development builds and `BlockLocalAddresses` otherwise. Choose a mode for your deployment:

| Mode | Behavior |
| --- | --- |
| `Warn` | Reports policy violations without blocking requests, except for unapproved cloud metadata addresses |
| `BlockLocalAddresses` | Blocks unapproved loopback (including mapped IPv4), IPv4 link-local `169.254/16`, IPv4 unspecified `0/8`, IPv6 link-local `fe80::/10`, and IPv6 unspecified `::`; warns about HTTP, missing host approval, and other private or special destinations |
| `BlockPrivateAddresses` | Also blocks other unapproved private or special addresses |
| `Enforce` | Blocks every policy violation, including HTTP and missing host approval |

Cloud metadata addresses require explicit host approval in every mode. Never approve metadata hosts in normal deployments. In the Development runtime environment, loopback and `host.docker.internal` have a development-host exception to host and address restrictions. HTTP is permitted for loopback in that environment; other hosts require `AllowInsecureHttp` to satisfy the HTTPS check. That setting defaults to `false` and is ignored outside Development.

`AllowedHosts` accepts exact hosts and wildcard subdomains such as `*.example.com`; the wildcard does not match the apex domain `example.com`. An `EventStoreAllowedHosts` entry replaces the global list, even when the entry is empty. In the example above, the `orders` store uses only `orders-api.internal` as its approved host. Add any required OAuth hosts to that store's list too.

Explicit approval permits a host's private addresses but does not waive the HTTPS check. Environment variable names start with `Cratis__Chronicle__ExternalServices__`, for example `Cratis__Chronicle__ExternalServices__Validation`.

Every capture request carries its event store. A route resolves against the External Service URL and must stay on the same origin: the same scheme, host, and port. Chronicle refuses a route to another origin before sending the request, keeping the service's authentication and headers on that service.

Redirects and system proxies are disabled, and DNS is validated when each connection opens. Requests to explicitly approved hosts close their connections after each request, even if the host currently resolves publicly. This prevents one event store's approval from being reused by another. Unapproved public destinations retain connection pooling in modes that permit them.

The policy covers HTTP capture polling and its OAuth discovery and token requests, not database endpoints. Before upgrading a deployment with local capture APIs, explicitly approve trusted hosts or select `Warn` as a migration setting. Review other private-network hosts before choosing `BlockPrivateAddresses` or `Enforce`. Use the [Kernel options reference](../configuration/chronicle-options.mdx#externalservices-kernel) to configure these settings.

## Using External Services from captures

Once the HTTP service is registered and its destinations are permitted, reference its name in the capture's API source. This excerpt uses `CustomersApi` for the connection and authentication, then selects `/customers` as the route to poll every five minutes:

```cdl
capture Customers
  source api
    api CustomersApi   # references the "CustomersApi" External Service
    route /customers
    poll 5m
  key customerId
  ...
```

Continue with [Capture Declaration Language](../captures/capture-declaration-language/index.md) to add append rules that define the events to emit when source data changes. Registering the External Service alone does not create or start a capture.
