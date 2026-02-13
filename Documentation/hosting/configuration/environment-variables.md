# Environment Variables

All configuration options can be set using environment variables with the prefix `Cratis__Chronicle__`. Use double underscores (`__`) to represent nested configuration sections.

## Example configuration

```json
{
  "managementPort": 8080,
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

| Variable | Description |
| --- | --- |
| Cratis__Chronicle__Port | gRPC service port |
| Cratis__Chronicle__ManagementPort | Management API port |
| Cratis__Chronicle__HealthCheckEndpoint | Health check endpoint path |
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
| Cratis__Chronicle__Events__Queues | Number of event queues |
| Cratis__Chronicle__Authentication__Authority | External OAuth authority URL |
| Cratis__Chronicle__Authentication__DefaultAdminUsername | Default admin username |
| Cratis__Chronicle__Authentication__DefaultAdminPassword | Default admin password |
| Cratis__Chronicle__Jobs__MaxParallelSteps | Maximum parallel job steps |
| Cratis__Chronicle__Tls__CertificatePath | TLS certificate path (PFX) |
| Cratis__Chronicle__Tls__CertificatePassword | TLS certificate password |

## Port Configuration

```bash
# gRPC port (default: 35000)
Cratis__Chronicle__Port=35000

# Management API port (default: 8080)
Cratis__Chronicle__ManagementPort=8080
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
# Number of appended event queues to use (default: 8)
Cratis__Chronicle__Events__Queues=8
```

## Authentication

```bash
# External OAuth authority URL (optional)
# When not set, uses internal OpenIdDict-based authority
Cratis__Chronicle__Authentication__Authority=https://your-oauth-provider.com

# Default admin username (default: "admin")
Cratis__Chronicle__Authentication__DefaultAdminUsername=admin

# Default admin password (default: "admin")
# Should be changed in production
Cratis__Chronicle__Authentication__DefaultAdminPassword=your-secure-password
```

