---
title: Chronicle Workbench local access
description: Open Chronicle Workbench at https://localhost:35000 with a development image, handle its local certificate, and sign in with the built-in account.
---
Chronicle Workbench is served at `https://localhost:35000` by the Development and Development-slim Docker images. Use the built-in administrator account to inspect your local Chronicle instance. If you have not started a container yet, follow the [Workbench local setup](index.mdx#start-locally) first.

## Accessing the Workbench

Both images bundle the Workbench, so you do not need to install it separately. Chronicle serves the browser interface and client connections on the same TLS-secured port, **35000**. With that port mapped to your machine, open:

```text
https://localhost:35000
```

Use `https://`, not `http://`, for this local setup.

> [!NOTE]
> When no certificate is configured, a development build generates a self-signed certificate in memory. Your browser may warn that it does not trust this certificate. Only proceed after confirming that you are connecting to your own local instance. A production deployment needs a configured TLS certificate; the development certificate is not a substitute.

## Logging In

For a local development image using the default account configuration, enter:

| Field    | Value           |
|----------|-----------------|
| Username | `Admin`         |
| Password | `ChangeMeNow!`  |

> [!NOTE]
> These credentials are for local development only. Never expose the development image or these credentials in a staging or production environment.

After signing in, select the event store and namespace you want to inspect. Continue with [inspecting a failed attempt's event](index.mdx#inspect-an-event-from-a-failed-attempt) when you need to see the event an observer could not process.
