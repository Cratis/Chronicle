<!-- cratis-ai-managed: skills/cratis-chronicle-client-kotlin/references/connection-string.md -->
# The connection string

This reference holds the connection string grammar, query keys, development credentials and TLS defaults.

### The connection string

`ChronicleConnectionString.kt` documents the grammar:

```
chronicle://<host>[:<port>][,<host>[:<port>]...][?<options>]
chronicle://<username>:<password>@<host>[:<port>][,...][?<options>]
chronicle+srv://<host>[:<port>][?<options>]
```

Default port is `35000`. Recognized query keys, lowercased at parse time
(`ChronicleConnectionString` parser): `disabletls`, `skiptlsvalidation`,
`loadbalancer`, `srvnameserver`. `apiKey` is parsed but rejected at client
creation with `IllegalArgumentException`: the kernel has no API-key authentication.
Use `user:password@`. Omitting credentials selects `chronicle-dev-client` /
`chronicle-dev-secret`; these are development credentials, not a production setup. `ChronicleConnectionString.parse(String)` is on the companion, and `ChronicleConnectionString.DEVELOPMENT` points at
`localhost:35000` with the `chronicle-dev-client` / `chronicle-dev-secret`
credentials.

> **TLS is on, certificate validation is off by default** (`disableTls = false`,
> `skipTlsValidation = true`), for a self-signed development kernel.
> **Production must set `?skipTlsValidation=false`** for platform trust-store
> chain validation (`ChronicleConnectionString.createCredentials`).
