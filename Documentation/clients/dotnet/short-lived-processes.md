---
title: Short-lived processes
description: Use the .NET client from command line tools, functions and jobs that connect, do a little work and exit.
---

A command line tool, a function or a job connects, appends or reads a few events and exits. It does not want a long-lived keep-alive stream or a watchdog, and it does not host reactors or reducers. The short-lived preset configures the client for exactly that.

## Using the preset

```csharp
var options = ChronicleOptions.ForShortLivedProcess("chronicle://localhost:35000");
using var client = new ChronicleClient(options);
var eventStore = await client.GetEventStore("my-store");

await eventStore.EventLog.Append("source", new SomethingHappened());
```

The preset sets `SkipKeepAlive` and a `RegistrationMode`. It leaves `AutoDiscoverAndRegister` and `SkipCompatibilityCheck` alone, so discovery and the compatibility check keep working as usual. To apply it to options you already have, call `UseShortLivedProcess()`:

```csharp
var options = new ChronicleOptions().UseShortLivedProcess(RegistrationMode.EventTypesOnly);
```

## What the client does not do

With keep-alive skipped the kernel has no live connection to deliver events on. The client therefore:

- opens no keep-alive stream and no watchdog
- does not register reactors, reducers, read model reactors or occurrences, and does not subscribe to external event stores
- throws `ClientObserversRequireKeepAlive` if you register a reactor or reducer explicitly, or nominate one through `ExplicitArtifacts`

Appending and reading events, and the rest of the client, work as before. Host reactors and reducers in a long-running service that does not skip keep-alive.

## Choosing how to register

`RegistrationMode` decides what the process registers when it connects.

| Mode | What happens |
| --- | --- |
| `Inline` | Registers everything that is discovered. The default. |
| `EventTypesOnly` | Ensures the event store exists and registers the event types only. |
| `AtDeployment` | Registers nothing. Another process of the application registered the artifacts at deployment time. |

Use `EventTypesOnly` or `AtDeployment` so a short-lived process does not pay for registering projections, constraints and read models on every start. `RegistrationMode` only has an effect when `AutoDiscoverAndRegister` is enabled.

## From a connection string

The same keep-alive behavior is available from the connection string with `skipKeepAlive=true`, and from the builder with `WithKeepAliveSkipped()`. See [connection strings](../../connection-strings/server.md).
