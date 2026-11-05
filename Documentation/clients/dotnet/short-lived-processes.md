---
title: Cratis Chronicle .NET clients for short-lived processes
description: Configure Cratis Chronicle .NET clients for tools, functions, and jobs that append or read events without hosting long-running observers.
---

Use the Cratis Chronicle .NET short-lived preset for a tool, function, or job that connects, appends or reads events, and exits without hosting reactors or reducers. It disables the keep-alive stream and watchdog that long-running clients use to detect a lost connection.

## Using the preset

In an application referencing `Cratis.Chronicle`, use `ChronicleOptions.ForShortLivedProcess`
from the `Cratis.Chronicle` namespace. This example assumes the local kernel is running and
`SomethingHappened` is one of your application's declared event types:

```csharp
var options = ChronicleOptions.ForShortLivedProcess("chronicle://localhost:35000");
using var client = new ChronicleClient(options);
var eventStore = await client.GetEventStore("my-store");

await eventStore.EventLog.Append("source", new SomethingHappened());
```

`ForShortLivedProcess` sets `SkipKeepAlive` and defaults `RegistrationMode` to `Inline`. It leaves
`AutoDiscoverAndRegister` and `SkipCompatibilityCheck` unchanged, so it does not disable discovery or
the compatibility check. For options you are configuring directly, call `UseShortLivedProcess()`;
this example also limits registration to event types:

```csharp
var options = new ChronicleOptions().UseShortLivedProcess(RegistrationMode.EventTypesOnly);
```

## What the client does not do

Without keep-alive, the kernel has no live connection on which to deliver events to this process.
The client therefore:

- opens no keep-alive stream and no watchdog
- does not register reactors, reducers, read model reactors or occurrences, and does not subscribe to external event stores
- throws `ClientObserversRequireKeepAlive` if you register a reactor or reducer explicitly, or nominate one through `ExplicitArtifacts`

You can still append and read events. Host reactors and reducers in a long-running service that
keeps keep-alive enabled; they cannot receive events through this short-lived client.

## Choosing how to register

`RegistrationMode` decides what the process registers when it connects.

| Mode | What happens |
| --- | --- |
| `Inline` | Registers discovered artifacts except the client-hosted observers excluded above. The default. |
| `EventTypesOnly` | Ensures the event store exists and registers the event types only. |
| `AtDeployment` | Registers nothing; uses artifacts registered by another process at deployment time. |

Choose `EventTypesOnly` when the process needs to register its events but should not register
projections, constraints, and read models on every start. Choose `AtDeployment` when another process
has already registered the definitions; [register definitions as a deployment step](./explicit-artifacts#register-definitions-as-a-deployment-step)
explains that workflow. `RegistrationMode` only has an effect when `AutoDiscoverAndRegister` is enabled.

## From a connection string

The same keep-alive behavior is available from the connection string with `skipKeepAlive=true`, and from the builder with `WithKeepAliveSkipped()`. See [connection strings](../../connection-strings/server.md).
