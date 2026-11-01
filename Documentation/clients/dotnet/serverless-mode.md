---
title: Serverless mode
description: Run the .NET client in a function or another scaled-to-zero host with one switch, and see what it does and does not support yet.
---

A function that scales to zero cannot hold a keep-alive stream or open observer streams. Serverless mode is one switch that configures the client for that kind of host, on top of the [short-lived process](./short-lived-processes.md) behavior.

:::caution[Preview]
The serverless surface is experimental. Using it reports the compiler diagnostic `CHRX0001`, which you acknowledge by suppressing it in your project. See [Compatibility](#compatibility).

HTTP delivery is not available yet. In this version serverless mode suits functions that append and read events. An observer that resolves to HTTP delivery fails at startup with `HttpDeliveryNotAvailable`, so a reactor or reducer does not run in a serverless client until a later version adds the receiver.
:::

## Compatibility

The serverless surface is experimental and marked with `[Experimental("CHRX0001")]`. It consists of:

- the options: `ChronicleOptions.Serverless`, `UseServerless` and `ForServerless`
- the settings types, including `ChronicleServerlessOptions` and `ServerlessDeliveryOptions`
- `DeliveryAttribute`, `ObserverDelivery`, `HttpDeliveryNotAvailable` and `InvalidServerlessConfiguration`
- the connection members `ChronicleConnectionStringBuilder.Serverless`, `ChronicleConnectionString.Serverless` and `WithServerless()`
- the `serverless=true` connection string key, which follows the same rule

The policy:

- The surface may change in a minor release until it graduates.
- It graduates when HTTP delivery ships (#5116 and #5119) and the partition fencing of decision 0018 has shipped, and one minor release has passed with no breaking change to the surface. Then `CHRX0001` is removed.
- Every change to it is listed in the release notes, marked `(part of #5118)`.
- A kernel that is older than the client serves append and read. HTTP delivery fails closed whatever version the kernel is, because the client refuses to register HTTP-delivered observers.
- With serverless mode off, which is the default, the client is covered by the normal compatibility rules.

HTTP delivery in its first version is live-only: it delivers new events, catch-up and retries, and does not replay. See [decision 0020](https://github.com/Cratis/Chronicle/blob/main/decisions/0020-kernel-http-delivery-contract.md). Because Chronicle does not re-push or erase what a receiver stored, see [Data delivered to webhooks and HTTP receivers](../../compliance/erasing-a-subject.mdx#data-delivered-to-webhooks-and-http-receivers) for the erasure procedure.

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

HTTP delivery fails closed until the kernel delivery envelope (#5116) and signing (#5117) ship. This includes fluent reactors registered through `ExplicitArtifacts.RegisterReactor`: an HTTP override fails at startup, even when `ReceiverUrl` is configured. The client does not silently fall back to a stream.

`DefaultDelivery` is `Http` by default, so set it to `Stream` if the host also contains observers you want skipped rather than refused. Observer identifiers that contain a colon cannot be used as configuration keys. A configured key that matches no observer is logged as a warning.

## Delivery timeout

HTTP reactors and reducers use `Serverless:Delivery:Timeout`, 30 seconds by default, bound from `Cratis:Chronicle:Serverless:Delivery:Timeout`. The kernel will send the deadline in the `Cratis-Delivery-Deadline` header. Webhook observers keep the kernel's `Cratis:Chronicle:Webhooks:RequestTimeoutSeconds`, 60 seconds by default. Neither setting applies to the other kind of observer, and there is no per-observer override in this version.

Set the function's execution timeout at or below the delivery timeout. A front door can answer before the function stops: an API Gateway HTTP API answers after about 30 seconds and the Azure Functions load balancer after 230 seconds, while the function keeps running. The kernel retries those answers while the first execution is still running.

The option has no effect until HTTP delivery ships.

## Startup validation

Serverless mode lists every problem at once as an `InvalidServerlessConfiguration`:

- `ProgramIdentifier` must be set to a stable name.
- `ReceiverUrl`, when set, must be an absolute URL. It must use `https` outside Development.
- `Delivery:Timeout` must be greater than zero.
- `Auth:Type` of `None` requires `Auth:AllowUnauthenticated` to be `true`.
- `Registration` of `Deploy` requires `DeploymentGeneration` outside Development.
- `Registration` of `OnColdStart` outside Development requires `AllowOnColdStartOutsideDevelopment`.

An injected connection must also skip keep-alive.

## Registration

With `Deploy`, another process registered the artifacts when the application was deployed, and the function registers nothing. Set `DeploymentGeneration` to the generation that deployment used; see [explicit artifact registration](./explicit-artifacts.md). With `OnColdStart` the client registers the artifacts itself when it starts cold, which is meant for Development.

`DeploymentGeneration` is only validated in this version: nothing consumes it yet. Enforcement comes with the lazy fingerprint and generation check (#5111).

## Unsupported APIs and limitations

Serverless mode opens no keep-alive stream or watchdog. It skips client-hosted stream delivery for reactors and reducers, read model reactor registration, and occurrence lifecycle registration.

The following APIs are unsupported in serverless mode. The client does not disable them or provide a fallback:

- Read model watch and observable APIs, including `IReadModels.Watch<TReadModel>()` and `GetWatcherFor<TReadModel>()`. Calling `ReadModelWatcher.Start` still opens a stream; serverless mode does not turn it into a snapshot or polling operation.
- Connection lifecycle APIs as a representation of function invocation or durable connection lifetime. Callbacks are not inert: the connection still calls `Lifecycle.Connected()` when keep-alive is skipped.
- Observer state streams. Use unary observer state queries instead; serverless mode does not convert streaming calls into unary queries.
- Arc live queries. Serverless mode does not convert them to snapshot or polling mode. Use non-streaming queries instead.

The startup log lists skipped work and unsupported APIs, including the stream-delivered observers it skipped. These messages describe limitations; they do not enforce restrictions on unsupported APIs.

When serverless mode is off and the process looks like a function host (for example `AWS_LAMBDA_FUNCTION_NAME`, `FUNCTIONS_WORKER_RUNTIME`, `K_SERVICE` or `CONTAINER_APP_NAME` is set) and observers would open streams, the client logs a warning suggesting the mode.

## Authenticating to the kernel

Prefer an API key in the connection string. The OAuth token cache lives on the local file system, which is ephemeral in a function, so every cold start would fetch a new token. A kernel on a private network must be reachable from the function, for example through a VPC and NAT.
