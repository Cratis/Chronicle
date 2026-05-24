---
name: add-traces
description: Use this skill when asked to add OpenTelemetry tracing to a class in a Cratis Chronicle Kernel project. Produces `*Traces.cs` companion files using the `[Span]` source-generator pattern from `Cratis.Traces`, registers `IActivitySource<T>` as a keyed DI service, and injects it into the target class.
---

# Adding Traces to a Class

Chronicle uses the **Fundamentals Traces** pattern from `Cratis.Traces`. Never call `System.Diagnostics.ActivitySource` directly. Always use the source-generator `[Span]` attribute and `IActivitySource<T>`.

## Overview

1. Create a `*Traces.cs` companion file with `[Span]` methods.
2. Register `IActivitySource<T>` as a keyed singleton in `ChronicleMetersExtensions`.
3. Inject `[FromKeyedServices(WellKnown.MeterName)] IActivitySource<TTarget>` into the class.
4. Call the generated extension methods and apply tags via `TagExtensions`.
5. Update specs to pass `new ActivitySource<T>()` (not a substitute).

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

Open `Source/Kernel/Core/Setup/ChronicleMetersExtensions.cs` and add:

```csharp
services.AddActivitySource<MyClass>();
```

The private helper method is:

```csharp
static IServiceCollection AddActivitySource<TTarget>(this IServiceCollection services)
{
    services.AddKeyedSingleton<IActivitySource<TTarget>>(WellKnown.MeterName, (_, _) =>
        new ActivitySource<TTarget>(ChronicleActivity.Source));
    return services;
}
```

Add the necessary `using` directives:
```csharp
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Traces;
```

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

### Traditional constructor (non-grain class)

```csharp
/// <param name="activitySource">The <see cref="IActivitySource{T}"/> for tracing.</param>
public MyClass(
    ...
    IActivitySource<MyClass> activitySource,
    ...)
{
    _activitySource = activitySource;
    ...
}

readonly IActivitySource<MyClass> _activitySource;
```

> Non-grain classes constructed manually (e.g., in service factories) must use `sp.GetRequiredKeyedService<IActivitySource<MyClass>>(WellKnown.MeterName)` when keyed, or a plain `new ActivitySource<MyClass>(ChronicleActivity.Source)` if wired inline.

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
- [ ] `ChronicleMetersExtensions.AddChronicleMeters` calls `services.AddActivitySource<T>()`
- [ ] `AddActivitySource<T>` private helper exists
- [ ] Class constructor injects `IActivitySource<T>` (keyed or non-keyed as appropriate)
- [ ] Span methods called with `span?.Activity?.Tag(...)` null-safe pattern
- [ ] `using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;` added for `TagExtensions`
- [ ] `using Cratis.Traces;` added for `ActivitySource<T>`, `IActivitySource<T>`
- [ ] Specs updated to use `new ActivitySource<T>()` instead of `Substitute.For<...>()`
- [ ] Build passes with no errors
- [ ] Specs pass
