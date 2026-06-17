---
name: add-traces
description: Use this skill when asked to add OpenTelemetry tracing to a class in a Cratis Chronicle Kernel project. Produces `*Traces.cs` companion files using the `[Span]` source-generator pattern from `Cratis.Traces`, registers `IActivitySource<T>` as a keyed DI service, and injects it into the target class.
---

# Adding Traces to a Class

Chronicle uses the **Fundamentals Traces** pattern from `Cratis.Traces`. Never call `System.Diagnostics.ActivitySource` directly. Always use the source-generator `[Span]` attribute and `IActivitySource<T>`.

## Overview

1. Create a `*Traces.cs` companion file with `[Span]` methods.
2. Register `IActivitySource<T>` via `services.AddNamedActivitySource(WellKnown.MeterName)` in `ChronicleMetersExtensions` (one call covers all types via open-generic keyed DI).
3. Inject `[FromKeyedServices(WellKnown.MeterName)] IActivitySource<TTarget>` into the class.
4. Call the generated extension methods and apply tags via `TagExtensions`.
5. Update specs to pass `new ActivitySource<T>()` (not a substitute).

---

## DI registration architecture

Chronicle's `ActivitySource` follows the same keyed DI pattern as `Meter`:

| Layer | Meter | ActivitySource |
|---|---|---|
| Shared well-known instance (keyed by `WellKnown.MeterName`) | `Meter("Cratis.Chronicle")` | `System.Diagnostics.ActivitySource("Cratis.Chronicle")` |
| Per-type wrapper (keyed by `WellKnown.MeterName`) | `IMeter<T>` / `Meter<T>` | `IActivitySource<T>` / `ActivitySource<T>` |
| Registered by | `services.AddNamedMeter(WellKnown.MeterName)` | `services.AddNamedActivitySource(WellKnown.MeterName)` |

Both calls are made from `ChronicleMetersExtensions.AddChronicleMeters()`. They use open-generic keyed DI (`TryAddKeyedSingleton(typeof(IMeter<>), name, typeof(Meter<>))`), so any new type added to a keyed-injected class is automatically covered — **no per-type registration step is needed**.

---

## Step 1 — Create a `*Traces.cs` companion file

Each class that needs tracing gets its own `*Traces.cs` file in the same folder.

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Traces;

namespace Cratis.Chronicle.<Namespace>;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class <ClassName>Traces
{
    [Span("cratis.chronicle.<domain>.<operation>", ActivityKind.Internal)]
    internal static partial IActivityScope<<ClassName>> <OperationName>(this IActivitySource<<ClassName>> source);
}
```

**Rules:**
- File name: `<ClassName>Traces.cs`, next to `<ClassName>.cs`.
- Class name: `<ClassName>Traces` (partial, static, internal).
- One `[Span]` method per traced operation.
- Span names follow `cratis.chronicle.<domain>.<operation>` (lower_snake_case).
- Use `ActivityKind.Internal` for grain-to-grain/internal calls.
- Use `ActivityKind.Server` for gRPC service entry points.
- No parameters in `[Span]` methods — tags are set manually via `TagExtensions`.

---

## Step 2 — Register `IActivitySource<T>` in DI

### 2a — Open-generic registration (no per-type step needed)

`ChronicleMetersExtensions.AddChronicleMeters()` calls:

```csharp
services.AddNamedActivitySource(WellKnown.MeterName);
```

This uses `TryAddKeyedSingleton(typeof(IActivitySource<>), name, typeof(ActivitySource<>))`, so any class
that injects `[FromKeyedServices(WellKnown.MeterName)] IActivitySource<T>` is automatically resolved — no
extra registration line is required.

> **Note:** `AddNamedMeter` and `AddNamedActivitySource` are currently provided by a local compatibility shim
> (`NamedDiagnosticsServiceCollectionExtensions.cs`) that mirrors the Fundamentals upstream API. When
> `Cratis.Fundamentals` is upgraded to a version that includes `DiagnosticsServiceCollectionExtensions`,
> delete that shim file and bump the version in `Directory.Packages.props`.

---

## Step 3 — Inject `IActivitySource<T>` into the class

### Primary constructor (grain / primary constructor class)

```csharp
/// <param name="activitySource">The <see cref="IActivitySource{T}"/> for tracing.</param>
public class MyClass(
    ...
    [FromKeyedServices(WellKnown.MeterName)] IActivitySource<MyClass> activitySource,
    ...)
```

### Traditional constructor (non-grain class, registered in DI)

```csharp
readonly IActivitySource<MyClass> _activitySource;

public MyClass(
    ...
    IActivitySource<MyClass> activitySource,
    ...)
{
    _activitySource = activitySource;
}
```

> Non-grain classes constructed **manually** (e.g., in service factories) must use:
> ```csharp
> sp.GetRequiredKeyedService<IActivitySource<MyClass>>(WellKnown.MeterName)
> ```

---

## Step 4 — Use the generated span methods and apply tags

```csharp
using var span = activitySource.MyOperation(); // or _activitySource.MyOperation()
span?.Activity?.Tag(someEventStore);
span?.Activity?.Tag(someNamespace);
span?.Activity?.Tag(someObserverKey);
try
{
    // ... work ...
}
catch (Exception ex)
{
    span?.Activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    throw;
}
```

**Always use null-conditional `span?.Activity?.Tag(...)`** because the source generator's `IActivityScope<T>` can be null when no listener is attached (e.g., in tests).

### Available `TagExtensions` (in `Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing`)

| Method signature | Tags set |
|---|---|
| `Tag(EventStoreName)` | `cratis.eventstore.name` |
| `Tag(EventStoreNamespaceName)` | `cratis.eventstore.namespace` |
| `Tag(EventSequenceId)` | `cratis.eventsequence.id` |
| `Tag(EventType)` | `cratis.eventtype.id` |
| `Tag(EventSourceType, EventSourceId)` | `cratis.eventsource.type`, `cratis.eventsource.id` |
| `Tag(ObserverId)` | `cratis.observer.id` |
| `Tag(ObserverType)` | `cratis.observer.type` |
| `Tag(ConnectionId)` | `cratis.connection.id` |
| `Tag(ObserverKey)` | observer id, event sequence id, namespace, event store |
| `Tag(ConnectedObserverKey, ObserverType?)` | observer id, event sequence id, connection id, namespace, event store, type |

Add new overloads to `TagExtensions.cs` for any domain concept not already listed.

---

## Step 5 — Update specs

In specs, **never** use `Substitute.For<IActivitySource<T>>()`. The source-generator accesses `.ActualSource` which NSubstitute returns as null, causing `NullReferenceException`. Instead, use:

```csharp
new ActivitySource<MyClass>()
```

Add `using Cratis.Traces;` where needed.

### For Orleans TestKit grains

```csharp
_silo.AddKeyedService<IActivitySource<Observer>>(WellKnown.MeterName, new ActivitySource<Observer>());
```

### For manually instantiated classes

```csharp
new MyClass(
    ...,
    new ActivitySource<MyClass>(),
    ...);
```

---

## Checklist

- [ ] `<ClassName>Traces.cs` created with `[Span]` methods
- [ ] No per-type DI registration needed — open-generic `AddNamedActivitySource` covers all types automatically
- [ ] Class constructor injects `IActivitySource<T>` (keyed with `[FromKeyedServices(WellKnown.MeterName)]` or non-keyed for non-grain DI)
- [ ] Span methods called with `span?.Activity?.Tag(...)` null-safe pattern
- [ ] `using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;` added for `TagExtensions`
- [ ] `using Cratis.Traces;` added for `ActivitySource<T>`, `IActivitySource<T>`
- [ ] Specs updated to use `new ActivitySource<T>()` instead of `Substitute.For<...>()`
- [ ] Build passes with no errors
- [ ] Specs pass
