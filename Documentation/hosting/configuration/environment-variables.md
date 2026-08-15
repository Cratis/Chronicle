# Environment Variables

All configuration options can be set using environment variables with the prefix `Cratis__Chronicle__`. Use double underscores (`__`) to represent nested configuration sections.

## Example configuration

```json
{
  "port": 35000,
  "features": {
    "api": true,
    "workbench": true
  },
  "storage": {
    "type": "MongoDB",
    "connectionDetails": "mongodb://localhost:27017"
  }
}
```

## Variables

The table below covers the variables most deployments need. It is not the complete option surface — every
property on Chronicle's options types has an environment-variable form, built by joining the section names
with `__`. When a setting is missing here, find it on its own configuration page and translate the JSON path
the same way (`compliance.encryption.migrateFromDefaultStorage` becomes
`Cratis__Chronicle__Compliance__Encryption__MigrateFromDefaultStorage`).

| Variable | Description |
| --- | --- |
| Cratis__Chronicle__Port | The single Chronicle port — gRPC (HTTP/2) and HTTP/1.1 |
| Cratis__Chronicle__HealthCheckEndpoint | Health check endpoint path |
| Cratis__Chronicle__Health__Port | Dedicated HTTP/1.1 port for the health endpoint |
| Cratis__Chronicle__Health__Tls | Whether the dedicated health port uses TLS (default `true`) |
| Cratis__Chronicle__Features__Api | Enable REST API endpoint |
| Cratis__Chronicle__Features__Workbench | Enable Workbench UI |
| Cratis__Chronicle__Features__ChangesetStorage | Enable changeset storage |
| Cratis__Chronicle__Features__OAuthAuthority | Enable internal OAuth authority |
| Cratis__Chronicle__Storage__Type | Storage provider type |
| Cratis__Chronicle__Storage__ConnectionDetails | Storage connection string |
| Cratis__Chronicle__Observers__SubscriberTimeout | Observer subscriber timeout in seconds |
| Cratis__Chronicle__Observers__MaxRetryAttempts | Maximum retry attempts for observers |
| Cratis__Chronicle__Observers__BackoffDelay | Initial observer backoff delay in seconds |
| Cratis__Chronicle__Observers__ExponentialBackoffDelayFactor | Exponential backoff multiplier |
| Cratis__Chronicle__Observers__MaximumBackoffDelay | Maximum observer backoff delay in seconds |
| Cratis__Chronicle__ReadModels__ReplayedVersionsToKeep | Number of replay-generated read model versions to keep |
| Cratis__Chronicle__Events__Queues | Number of event queues |
| Cratis__Chronicle__Authentication__Authority | External OAuth authority URL |
| Cratis__Chronicle__Authentication__DefaultAdminUsername | Default admin username |
| Cratis__Chronicle__Authentication__AdminUser__Username | Bootstrap admin username (falls back to the default admin username when empty) |
| Cratis__Chronicle__Authentication__AdminUser__Password | Bootstrap admin password, hashed on first startup |
| Cratis__Chronicle__Authentication__AdminUser__Email | Bootstrap admin email address |
| Cratis__Chronicle__Authentication__AdminUser__RequirePasswordChangeOnFirstLogin | Force a password change on the admin's first login |
| Cratis__Chronicle__Jobs__MaxParallelSteps | Maximum parallel job steps |
| Cratis__Chronicle__Clustering__Type | Clustering provider — `Localhost` (default) or `MongoDB` |
| Cratis__Chronicle__Clustering__ClusterId | Orleans cluster id, identical on every node |
| Cratis__Chronicle__Clustering__ServiceId | Orleans service id, identical on every node |
| Cratis__Chronicle__Clustering__SiloPort | Orleans silo port (default 11111) |
| Cratis__Chronicle__Clustering__GatewayPort | Orleans gateway port (default 30000) |
| Cratis__Chronicle__Clustering__AdvertisedIP | IP address this node advertises to the cluster |
| Cratis__Chronicle__Tls__CertificatePath | TLS certificate path (PFX) |
| Cratis__Chronicle__Tls__CertificatePassword | TLS certificate password |
| Cratis__Chronicle__EncryptionCertificate__CertificatePath | Encryption certificate path (PFX) — OAuth keys, webhook credentials, Data Protection keys |
| Cratis__Chronicle__EncryptionCertificate__CertificatePassword | Encryption certificate password |
| Cratis__Chronicle__EncryptionCertificate__Previous__0__CertificatePath | Path to a certificate that was active before, kept for decryption only during a [rotation](../encryption-certificate.md#rotating-the-certificate). Index upwards for more than one |
| Cratis__Chronicle__EncryptionCertificate__Previous__0__CertificatePassword | Password for that certificate |
| OTEL_EXPORTER_OTLP_ENDPOINT | OTLP receiver endpoint for telemetry export |
| OTEL_EXPORTER_OTLP_PROTOCOL | OTLP export protocol (`grpc` or `http/protobuf`) |
| OTEL_EXPORTER_OTLP_HEADERS | Additional headers for the OTLP exporter |
| OTEL_SERVICE_NAME | Service name reported to the telemetry backend |

## Port Configuration

```bash
# The single Chronicle port — gRPC (HTTP/2) plus HTTP/1.1 (default: 35000)
Cratis__Chronicle__Port=35000
```

## Health Check Endpoint

```bash
# Health check endpoint path (default: /health)
Cratis__Chronicle__HealthCheckEndpoint=/health
```

## Feature Toggles

```bash
# Enable or disable API (default: true)
Cratis__Chronicle__Features__Api=true

# Enable or disable Workbench (default: true)
Cratis__Chronicle__Features__Workbench=true

# Enable or disable Changeset Storage (default: false)
Cratis__Chronicle__Features__ChangesetStorage=false

# Enable or disable internal OAuth authority (default: true)
# Automatically disabled when external authority is configured
Cratis__Chronicle__Features__OAuthAuthority=true
```

## Storage

```bash
# Storage type (e.g., "MongoDB")
Cratis__Chronicle__Storage__Type=MongoDB

# MongoDB connection string
Cratis__Chronicle__Storage__ConnectionDetails=mongodb://localhost:27017
```

## Observers

```bash
# Timeout in seconds for observer subscriber calls (default: 5)
Cratis__Chronicle__Observers__SubscriberTimeout=5

# Maximum retry attempts for failed partitions (0 = infinite, default: 10)
Cratis__Chronicle__Observers__MaxRetryAttempts=10

# Initial backoff delay in seconds (default: 1)
Cratis__Chronicle__Observers__BackoffDelay=1

# Exponential backoff factor (default: 2)
Cratis__Chronicle__Observers__ExponentialBackoffDelayFactor=2

# Maximum backoff delay in seconds (default: 600)
Cratis__Chronicle__Observers__MaximumBackoffDelay=600
```

## Events

```bash
# Number of appended event queues to use (default: 2)
Cratis__Chronicle__Events__Queues=2
```

## Read models

```bash
# Number of replay-generated read model versions to keep per read model (default: 1)
Cratis__Chronicle__ReadModels__ReplayedVersionsToKeep=1
```

## Authentication

```bash
# External OAuth authority URL (optional)
# When not set, uses internal OpenIdDict-based authority
Cratis__Chronicle__Authentication__Authority=https://your-oauth-provider.com

# Default admin username (default: "admin")
Cratis__Chronicle__Authentication__DefaultAdminUsername=admin

# Bootstrap the initial admin user on first startup. The password is hashed
# immediately on use and never retained. Supply it from your secret store.
Cratis__Chronicle__Authentication__AdminUser__Username=admin
Cratis__Chronicle__Authentication__AdminUser__Password=your-secure-password
Cratis__Chronicle__Authentication__AdminUser__RequirePasswordChangeOnFirstLogin=true
```

When `AdminUser__Password` is left empty, the admin user is created without a password and goes through the
initial password setup flow in the Workbench. See [Authentication](authentication.md) for the full flow.

## Encryption certificate

```bash
# Required in production - the internal OAuth authority refuses to start without it
Cratis__Chronicle__EncryptionCertificate__CertificatePath=/certs/encryption-cert.pfx
Cratis__Chronicle__EncryptionCertificate__CertificatePassword=your-certificate-password
```

See [Data Protection Key Encryption](../encryption-certificate.md) for generating and mounting the certificate.

## Clustering

```bash
# Clustering provider - must be MongoDB for every multi-node deployment
Cratis__Chronicle__Clustering__Type=MongoDB

# Identical on every node so they join the same cluster
Cratis__Chronicle__Clustering__ClusterId=chronicle
Cratis__Chronicle__Clustering__ServiceId=chronicle
```

See [Clustering](clustering.md) for the full set of clustering properties.

## Open Telemetry

```bash
# OTLP receiver endpoint (e.g. local Aspire Dashboard or OpenTelemetry Collector)
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317

# Export protocol: grpc (default) or http/protobuf
OTEL_EXPORTER_OTLP_PROTOCOL=grpc

# Additional headers, e.g. API keys for cloud backends
OTEL_EXPORTER_OTLP_HEADERS=x-api-key=your-api-key

# Override the service name reported to the telemetry backend
OTEL_SERVICE_NAME=Chronicle
```

See the [Open Telemetry](open-telemetry.md) configuration page for full details.
