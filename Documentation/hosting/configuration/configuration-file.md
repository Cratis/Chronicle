# Configuration File

Chronicle Server loads configuration from a `chronicle.json` file in the application root directory. In containers, this file is typically mounted at `/app/chronicle.json`.

## Example Configuration

```json
{
    "port": 35000,
    "healthCheckEndpoint": "/health",
    "features": {
        "api": true,
        "workbench": true,
        "changesetStorage": false,
        "oAuthAuthority": true
    },
    "storage": {
        "type": "MongoDB",
        "connectionDetails": "mongodb://localhost:27017"
    },
    "observers": {
        "subscriberTimeout": 5,
        "maxRetryAttempts": 10,
        "backoffDelay": 1,
        "exponentialBackoffDelayFactor": 2,
        "maximumBackoffDelay": 600
    },
    "readModels": {
        "replayedVersionsToKeep": 1
    },
    "events": {
        "queues": 8
    },
    "authentication": {
        "authority": null,
        "defaultAdminUsername": "admin",
        "adminUser": {
            "username": "admin",
            "password": "your-secure-password",
            "email": "admin@example.com",
            "requirePasswordChangeOnFirstLogin": true
        }
    },
    "tls": {
        "certificatePath": "/certs/chronicle.pfx",
        "certificatePassword": "your-password"
    },
    "encryptionCertificate": {
        "certificatePath": "/certs/encryption-cert.pfx",
        "certificatePassword": "your-password"
    },
    "identityProvider": {
        "certificate": {
            "enabled": true,
            "certificatePath": "/path/to/identity-provider.pfx",
            "certificatePassword": "your-password"
        }
    }
}
```

Note that the sections are **not** wrapped in `Cratis` / `Chronicle` here — Chronicle republishes everything it reads from `chronicle.json` under the `Cratis:Chronicle:` configuration path itself. That prefix belongs on environment variables only.

Environment variables can override any of these values. See [Configuration Precedence](configuration-precedence.md) for details.

> [!IMPORTANT]
> Keep passwords out of the file itself. Because every value above can be overridden by an environment variable,
> supply secrets that way from your secret store — see [Environment Variables](environment-variables.md).

| Section | Description |
| --- | --- |
| port, healthCheckEndpoint | Root properties for the port and health check path |
| features | Feature toggles for API, Workbench, and OAuth authority |
| storage | Storage provider configuration |
| observers | Retry and timeout settings for observers |
| readModels | Replay retention settings for replay-generated read model versions |
| events | Event queue configuration |
| authentication | Authentication, default admin username, and initial admin user bootstrap |
| tls | TLS certificate for the main Chronicle port — required in production |
| encryptionCertificate | Certificate protecting OAuth keys, webhook credentials, and Data Protection keys — required in production |
| identityProvider | Optional internal identity provider certificate settings |
