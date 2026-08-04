---
title: Observing artifact registration
description: Ask an event store whether its declared artifacts registered, instead of re-driving discovery and registration to find out.
---

Chronicle registers your artifacts for you. `RegisterAll` is wired to the connection lifecycle and runs whenever the connection comes up, which is the right default and stays the default.

What `IEventStore.Registration` adds is a way to ask afterwards how it went.

## Why you might need it

A read model that cannot be built is isolated: it is logged and skipped, so one broken read model no longer costs the rest of your read side. That is a good trade, but it turns a loud failure into a quiet one. The broken read model is simply absent afterwards, and nothing throws.

Without an outcome to read, these three situations look identical from outside:

- every artifact registered
- some registered, and the rest were dropped
- registration has not run yet

`Registration` tells them apart.

## Reading the outcome

```csharp
var outcome = eventStore.Registration;

if (!outcome.HasRun)
{
    // Registration has not completed yet - or has not been triggered at all.
}

foreach (var failed in outcome.Failures)
{
    Console.WriteLine($"{failed.ArtifactType.Name} did not register: {failed.Failure!.Message}");
}
```

| Member | What it answers |
| --- | --- |
| `HasRun` | Whether `RegisterAll` has completed at least once |
| `Artifacts` | Every declared projection artifact, with `IsRegistered` and the `Failure` that stopped it |
| `IsSuccess` | Registration has run and every declared artifact registered |
| `Failures` | Only the artifacts that did not register |

`RegistrationOutcome.NotRun` is the value before registration has completed.

## Waiting for it

Registration runs on the connection lifecycle, so a consumer that has just constructed a client may simply be early. `WaitForRegistration` waits for it to have run, with the same timeout shape as the other Chronicle wait helpers:

```csharp
using Cratis.Chronicle.Registrations;

var outcome = await eventStore.WaitForRegistration(TimeSpan.FromSeconds(10));

if (!outcome.IsSuccess)
{
    throw new InvalidOperationException("The read side did not come up.");
}
```

It waits for registration to have *run*, not to have succeeded — ask the returned outcome about that. If it has not run within the timeout, the wait throws `TaskCanceledException`.

## What it covers

`Registration` reports projection artifacts: the fluent `IProjectionFor<T>` implementations and the model-bound read models. Those are the artifacts whose registration round-trips to the kernel inside `RegisterAll`, so the outcome is observed rather than assumed.

Reactors and reducers are deliberately not reported. Their registration only opens a duplex stream and returns before the kernel has answered, so the client has nothing to report about them that would be a fact. To know a reactor or reducer is live, wait on its observer state instead — `WaitTillSubscribed` or `WaitTillActive` — which the kernel answers.

The outcome is read-only and decides nothing. Whether a failed artifact should abort start-up, fail a spec, or be tolerated is yours to decide.

:::caution
Do not use `IConnectionLifecycle.IsConnected` as a stand-in. It is set to `true` *before* the connected handlers run — registration is one of them — and only rolled back afterwards if one of them failed. Polling it races: it reads `true` while registration is still in flight, and while a registration about to be reported as failed is still running. It answers "connected", which is necessary and not sufficient.
:::
