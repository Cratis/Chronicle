<!-- cratis-ai-managed: skills/cratis-chronicle-configuration/references/environment-variables.md -->
# Environment variables

## Contents

- Naming rule
- Common variables
- Secrets
- Container example

## Naming rule

Every kernel option has an environment variable. Prefix `Cratis__Chronicle__`,
join section names with `__` (double underscore), keep the property name. The
JSON path `compliance.encryption.migrateFromDefaultStorage` becomes
`Cratis__Chronicle__Compliance__Encryption__MigrateFromDefaultStorage`. Names are
case-insensitive. List items take an index:
`Cratis__Chronicle__Webhooks__AllowedHosts__0`. Per-key dictionaries use the key
as a segment: `Cratis__Chronicle__Webhooks__EventStoreAllowedHosts__orders__0`.

An application's `Cratis:Chronicle` configuration section has the same
environment form (`Cratis__Chronicle__...`) but is read by the client in the
application process; set it on the application, not on the kernel container.

## Common variables

| Variable | Sets |
| --- | --- |
| `Cratis__Chronicle__Port` | Listener port (35000) |
| `Cratis__Chronicle__Tls__Enabled` | TLS on the listener (default `true`) |
| `Cratis__Chronicle__Tls__CertificatePath`, `...CertificatePassword` | Listener certificate (PFX) |
| `Cratis__Chronicle__Storage__Type`, `...ConnectionDetails` | Storage backend and connection |
| `Cratis__Chronicle__Features__Workbench`, `...Api` | Workbench and REST API |
| `Cratis__Chronicle__Authentication__Authority` | External identity provider issuer URL |
| `Cratis__Chronicle__Observers__SubscriberTimeout` | Observer subscriber timeout, seconds |
| `Cratis__Chronicle__Observers__MaxRetryAttempts` | Observer retries |
| `Cratis__Chronicle__Clustering__Type` | `Localhost`, `MongoDB` or `Sql` |
| `Cratis__Chronicle__Clustering__GatewayEnabled` | Orleans client gateway |
| `Cratis__Chronicle__Webhooks__Validation`, `Cratis__Chronicle__ExternalServices__Validation` | Destination validation mode |
| `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_SERVICE_NAME` | Telemetry export |

The Chronicle hosting documentation's environment variables page holds the
full table; read it for anything not listed here.

## Secrets

Several options have a `*File` form that reads the value from a file, for
example a mounted Kubernetes Secret: `Cratis__Chronicle__Storage__ConnectionDetailsFile`,
`...Sinks__Configurations__<name>__ConnectionDetailsFile`,
`...Neo4j__PasswordFile`. Set either the value or its file variant, not both.
Use these for connection strings, passwords and tokens; never commit the value.

## Container example

```yaml
services:
  chronicle:
    image: cratis/chronicle:<tested-release>
    environment:
      Cratis__Chronicle__Storage__ConnectionDetailsFile: /run/secrets/chronicle-storage
      Cratis__Chronicle__Tls__CertificatePath: /certs/chronicle.pfx
      Cratis__Chronicle__Tls__CertificatePassword: ${CHRONICLE_CERT_PASSWORD}
      Cratis__Chronicle__Features__Workbench: "false"
```

A pinned release tag, not `latest`, is for deployments. Topology, replica sets
and certificate provisioning are in `cratis-chronicle-hosting`.
