---
title: Cratis Chronicle .NET artifact registration outcomes
description: Read IEventStore.Registration to check projection registration, inspect failures, and wait for the current run without triggering registration again.
---

`IEventStore.Registration` reports whether Cratis Chronicle's .NET client registered its declared projection artifacts and what failed. Read it when you need to decide whether a service or test can proceed. With automatic registration enabled, `RegisterAll` runs when the connection comes up; inspecting the outcome does not trigger another run.

## Why you might need it

If a read model cannot be built, the client logs and skips it so the remaining projection artifacts can register. The registration call may therefore finish without throwing even though a model you need is absent.

Read `Registration` to distinguish a complete registration from a partial one or a run that has not finished. You can then choose whether the failure should stop startup, fail a test, or be tolerated.

## Reading the outcome

```csharp
var outcome = eventStore.Registration;

if (!outcome.HasRun)
{
    // Registration has not finished yet - or has not been triggered at all.
}

if (outcome.Failure is not null)
{
    // Registration ran and did not finish. Whatever registered before it stopped is still in Artifacts.
    Console.WriteLine($"Registration failed: {outcome.Failure.Message}");
}

foreach (var failed in outcome.Failures)
{
    Console.WriteLine($"{failed.ArtifactType.Name} did not register: {failed.Failure!.Message}");
}
```

| Member | What it answers |
| --- | --- |
| `HasRun` | Whether `RegisterAll` has finished at least once, successfully or not |
| `IsRegistering` | A registration run is in flight, or a disconnect is waiting for re-registration on reconnect |
| `Artifacts` | Every declared projection artifact, with `IsRegistered` and the `Failure` that stopped it |
| `Failure` | What stopped `RegisterAll` itself, or `null` when it finished |
| `IsSuccess` | Registration ran, finished, and every declared projection artifact registered |
| `Failures` | Only the artifacts that did not register |
| `Fingerprint` | The prepared artifact fingerprint from `RegisterDefinitions`; `NotSet` after `RegisterAll` |

`RegistrationOutcome.NotRun` is the value before registration has finished.

When automatic registration is enabled, a disconnect sets `IsRegistering` to `true` until re-registration finishes after reconnecting. Manual `RegisterAll` calls and background-retry attempts also set it while running. Between retry attempts, it is false and the last failure remains available. Other members retain the previous run's outcome during a new run, so check both `IsSuccess` and `IsRegistering` before treating registration as ready.

Use `Failures` to find individual projection artifacts that could not be built; those are skipped while registration continues. Use the outcome's singular `Failure` to find an exception that ended the whole run. That run still publishes an outcome, so a consumer waiting for registration receives the failure instead of waiting until timeout.

## Waiting for it

Immediately after constructing a client, registration may still be running. Use `WaitForRegistration` to wait for its outcome without starting another registration run:

```csharp
using Cratis.Chronicle.Registrations;

var outcome = await eventStore.WaitForRegistration(TimeSpan.FromSeconds(10));

if (!outcome.IsSuccess)
{
    throw new InvalidOperationException("The read side did not come up.");
}
```

The returned outcome can describe success or failure, which is why the example checks `IsSuccess`. During re-registration, the helper waits for the new outcome instead of returning the previous result. If registration does not finish within the timeout, it throws `TaskCanceledException`. A completed run that failed returns its outcome, carrying `Failure`.

## Discovery happens before registration

Before Chronicle can register artifacts, `DefaultClientArtifactsProvider` discovers their types through `ICanProvideAssembliesForDiscovery`. The first access to any artifact collection initializes all collections. Concurrent readers wait for discovery to finish, and later reads reuse the completed collections without scanning assemblies again.

If assembly initialization or type discovery throws, that access fails with the original exception. The provider does not publish the incomplete collections, and the next access retries initialization. This is a retry on access, not an automatic background retry.

If you supply custom assembly discovery, finish that work before reading the provider's artifact collections. Reading them again on the initializing thread throws `ReentrantClientArtifactsInitialization`: discovery cannot consume its own unfinished result. Avoid waiting inside discovery for another thread that reads artifacts too, because that reader waits for discovery to finish.

To inspect the types found during discovery, use the [discovery report](./discovery-report.md). Finding a type does not mean the kernel registered it; the outcome and observer-state helpers below answer that later question.

## What it covers

`Registration` reports projection artifacts: the fluent `IProjectionFor<T>` implementations and the model-bound read models. Those are the artifacts whose registration round-trips to the kernel inside `RegisterAll`, so the outcome is observed rather than assumed.

Reactor and reducer registration opens a two-way stream and returns before the kernel answers, so those observers are not included in the outcome. To check whether a reactor or reducer is live, wait for the kernel's observer state with `WaitTillSubscribed` or `WaitTillActive`.

The outcome is read-only. For automatic registration, the ASP.NET Core [readiness health check](./health-checks.md) requires a live connection, `Registration.IsSuccess`, and `IsRegistering` to be false. It reports a failure status for in-progress, incomplete, or failed registration; that page also covers hosts that turn automatic registration off.

:::caution
Do not use `IConnectionLifecycle.IsConnected` as a registration check. It becomes `true` before the connected handlers run, including registration, and is rolled back afterwards if a handler fails. Polling it can therefore report a connection while registration is still running or about to fail.
:::
