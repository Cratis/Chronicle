# Development

The Development and Development-slim Docker images include a built-in administrator account so you can start exploring immediately without any configuration.

## Accessing the Workbench

When Chronicle is running via the Development or Development-slim image, the Workbench is served on port **8080**. Open your browser and navigate to:

```text
http://localhost:8080
```

No separate installation or configuration is required — the Workbench starts automatically with the Kernel.

## Logging In

Use the following credentials on the login screen:

| Field    | Value           |
|----------|-----------------|
| Username | `Admin`         |
| Password | `ChangeMeNow!`  |

> [!NOTE]
> These credentials are for local development only. Never expose the development image or these credentials in a staging or production environment.
