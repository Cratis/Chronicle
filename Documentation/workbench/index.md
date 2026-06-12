# Workbench

The Workbench is a web-based tool built into the Chronicle Kernel that lets you inspect and interact with your event store directly in a browser. It is available when running the **Development** or **Development-slim** Docker images and is intended for local development workflows.

## Accessing the Workbench

When Chronicle is running via the Development or Development-slim image, the Workbench is served on port **8080**. Open your browser and navigate to:

```
http://localhost:8080
```

No separate installation or configuration is required — the Workbench starts automatically with the Kernel.

## Logging In

The development images ship with a built-in administrator account. Use the following credentials on the login screen:

| Field    | Value           |
|----------|-----------------|
| Username | `Admin`         |
| Password | `ChangeMeNow!`  |

> [!NOTE]
> These credentials are for local development only. Never expose the development image or these credentials in a staging or production environment.
