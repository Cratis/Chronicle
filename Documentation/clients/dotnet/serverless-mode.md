---
title: Serverless mode
description: Run the .NET client in a function or another scaled-to-zero host with one switch, and see what it does and does not support yet.
---

A function that scales to zero cannot hold a keep-alive stream or open observer streams. Serverless mode is one switch that configures the client for that kind of host, on top of the [short-lived process](./short-lived-processes.md) behavior.

:::caution
HTTP delivery is not available yet. In this version serverless mode suits functions that append and read events. An observer that resolves to HTTP delivery fails at startup with `HttpDeliveryNotAvailable`, so a reactor or reducer does not run in a serverless client until a later version adds the receiver.
:::

## What the switch does

When serverless mode is on, the client:

- skips the keep-alive connection, as in a short-lived process
- registers at deployment when `Registration` is `Deploy`, and inline when it is `OnColdStart`
- does not start the hosted occurrence lifecycle
- validates its serverless configuration when the options are resolved and again when the client starts, and refuses to start when it is not valid
- logs what it gives up, once, at startup

With the mode off, which is the default, nothing changes.

## Turning it on

The preset sets the mode, the program identifier and the receiver URL, and applies the short-lived behavior with registration at deployment:

```csharp
var options = ChronicleOptions.ForServerless(
    "chronicle://my-kernel:35000",
    "orders-functions",
    new Uri("https://orders.example.com"));

using var client = new ChronicleClient(options);
```

To apply it to options you already have, call `UseServerless("orders-functions")`. The receiver URL is optional.

The same settings bind from configuration under `Cratis:Chronicle:Serverless`:

```json
{
  "Cratis": {
    "Chronicle": {
      "ProgramIdentifier": "orders-functions",
      "Serverless": {
        "Mode": "Serverless",
        "ReceiverUrl": "https://orders.example.com",
        "ReceiverPath": "/chronicle/deliveries",
        "Registration": "Deploy",
        "DeploymentGeneration": 12,
        "DefaultDelivery": "Stream",
        "Auth": { "Type": "Signature" }
      }
    }
  }
}
```

The connection string can turn the mode on too, with `serverless=true`, or `WithServerless()` on the builder. This is a .NET client setting; the other clients ignore it. It is equal to setting `Serverless:Mode` to `Serverless`.

## Choosing how an observer is delivered

Each reactor and reducer resolves to one delivery, `Stream` or `Http`:

1. `Cratis:Chronicle:Serverless:Observers:<ObserverId>:Delivery` in configuration
2. `[Delivery(ObserverDelivery.Http)]` on the class
3. the mode default: `Stream` when serverless mode is off, `Serverless:DefaultDelivery` when it is on

| Mode | Resolved delivery | Result in this version |
| --- | --- | --- |
| Off | `Stream` | Runs as before |
| Serverless, resolves to `Stream` | `Stream` | Skipped, and listed in the startup log |
| Serverless, resolves to `Http` | `Http` | Fails at startup with `HttpDeliveryNotAvailable` |

`DefaultDelivery` is `Http` by default, so set it to `Stream` if the host also contains observers you want skipped rather than refused. Observer identifiers that contain a colon cannot be used as configuration keys. A configured key that matches no observer is logged as a warning.

## Startup validation

Serverless mode lists every problem at once as an `InvalidServerlessConfiguration`:

- `ProgramIdentifier` must be set to a stable name.
- `ReceiverUrl`, when set, must be an absolute `http` or `https` URL.
- `Auth:Type` of `None` requires `Auth:AllowUnauthenticated` to be `true`.
- `Registration` of `Deploy` requires `DeploymentGeneration` outside Development.
- `Registration` of `OnColdStart` outside Development requires `AllowOnColdStartOutsideDevelopment`.

An injected connection must also skip keep-alive.

## Registration

With `Deploy`, another process registered the artifacts when the application was deployed, and the function registers nothing. Set `DeploymentGeneration` to the generation that deployment used; see [explicit artifact registration](./explicit-artifacts.md). With `OnColdStart` the client registers the artifacts itself when it starts cold, which is meant for Development.

## What it gives up

The startup log lists the downgrades: stream reactors and reducers are skipped, read model watch and observable streams are unavailable, connection lifecycle events are inert, observer state queries are unary only, live queries run in snapshot or polling mode, and the hosted lifecycle occurrences are not started.

When serverless mode is off and the process looks like a function host (for example `AWS_LAMBDA_FUNCTION_NAME`, `FUNCTIONS_WORKER_RUNTIME`, `K_SERVICE` or `CONTAINER_APP_NAME` is set) and observers would open streams, the client logs a warning suggesting the mode.

## Authenticating to the kernel

Prefer an API key in the connection string. The OAuth token cache lives on the local file system, which is ephemeral in a function, so every cold start would fetch a new token. A kernel on a private network must be reachable from the function, for example through a VPC and NAT.
