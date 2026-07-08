# Development

The Development and Development-slim Docker images include a built-in administrator account so you can start exploring immediately without any configuration.

## Accessing the Workbench

When Chronicle is running via the Development or Development-slim image, the Workbench is served on the single Chronicle port, **35000** — the same port your clients connect to. Everything now goes through one TLS-secured port, so the Workbench is HTTPS. Open your browser and navigate to:

```text
https://localhost:35000
```

No separate installation or configuration is required — the Workbench starts automatically with the Kernel.

> [!NOTE]
> In development, when you haven't configured a certificate, Chronicle generates a self-signed one in memory so the secure port works out of the box. Your browser will show a certificate warning the first time — accept it to continue. In production you supply a real certificate, and the warning goes away.

## Logging In

Use the following credentials on the login screen:

| Field    | Value           |
|----------|-----------------|
| Username | `Admin`         |
| Password | `ChangeMeNow!`  |

> [!NOTE]
> These credentials are for local development only. Never expose the development image or these credentials in a staging or production environment.
