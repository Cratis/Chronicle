# Configuration File

Chronicle Server loads configuration from a `chronicle.json` file in the application root directory. In containers, this file is typically mounted at `/app/chronicle.json`.

## Example Configuration

```json
{
    "managementPort": 8080,
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
    "events": {
        "queues": 8
    },
    "authentication": {
        "authority": null,
        "defaultAdminUsername": "admin",
        "defaultAdminPassword": "admin"
    }
}
```

Environment variables can override any of these values. See [Configuration Precedence](configuration-precedence.md) for details.

| Section | Description |
| --- | --- |
| managementPort, port, healthCheckEndpoint | Root properties for ports and health check path |
| features | Feature toggles for API, Workbench, and OAuth authority |
| storage | Storage provider configuration |
| observers | Retry and timeout settings for observers |
| events | Event queue configuration |
| authentication | Authentication and default admin settings |

