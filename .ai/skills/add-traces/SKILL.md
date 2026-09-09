---
name: add-traces
description: Add OpenTelemetry tracing to a class in the Chronicle repository using the Cratis.Traces [Span] source generator, keyed IActivitySource<T> injection, and the Chronicle tag extensions. Use when instrumenting Chronicle kernel or client internals; this is contributor guidance for the Chronicle repository itself, not for an application built on Chronicle.
---

# Add tracing to a Chronicle class

Chronicle instruments itself with the **Fundamentals traces** pattern from
`Cratis.Traces`. Never call `System.Diagnostics.ActivitySource` directly. Declare
spans with the `[Span]` source generator and take an `IActivitySource<T>` on the
class being traced.

This is contributor guidance for the **Chronicle repository**. An application
built on Chronicle instruments itself with ordinary OpenTelemetry; nothing here
applies to it.

## Verified product sources

This skill is verified against these exact sources:

| Package | Purpose |
| --- | --- |
| `Cratis.Fundamentals` | `Cratis.Traces.SpanAttribute`, `IActivitySource<T>`, `IActivityScope<T>`, `AddNamedMeter`, `AddNamedActivitySource` |
| `Cratis.Chronicle` (kernel and clients) | `ChronicleMetersExtensions`, `WellKnown.MeterName`, the tag extensions |

Confirm the pinned `Cratis.Fundamentals` version in the repository's package
management before relying on a member.

## The shape

1. Declare the spans in a `<ClassName>Traces.cs` companion file.
2. Take `IActivitySource<TClass>` on the class.
3. Open a scope with the generated method and tag it.
4. Update the specifications to supply a real activity source.

No per-type dependency-injection registration is needed — the registration is
open-generic.

## Step 1 — Declare the spans

Each traced class gets its own `<ClassName>Traces.cs`, in the same folder as
`<ClassName>.cs`.

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
    internal static partial IActivityScope<<ClassName>> <OperationName>(
        this IActivitySource<<ClassName>> source);
}
```

The attribute is `Cratis.Traces.SpanAttribute`:

```csharp
[AttributeUsage(AttributeTargets.Method)]
public sealed class SpanAttribute(string name, ActivityKind kind = ActivityKind.Internal) : Attribute
```

Conventions:

- File named `<ClassName>Traces.cs`, next to `<ClassName>.cs`.
- Class `<ClassName>Traces`, `internal static partial`.
- One `[Span]` method per traced operation, declared `internal static partial`
  and returning `IActivityScope<TClass>`, as an extension on
  `IActivitySource<TClass>`.
- **Span names are lower snake case, dot separated**, and the prefix says which
  side of the wire the span is on: `cratis.chronicle.<domain>.<operation>` in the
  kernel, `client.<domain>.<operation>` in the client.
- `ActivityKind.Internal` for internal and grain-to-grain work,
  `ActivityKind.Server` for gRPC service entry points, and `Client` or `Consumer`
  on the client side as appropriate.

A `[Span]` method **may take parameters** after the source — the client's
sequence traces pass the event store, namespace, and sequence identifiers that
way. In the kernel the established style is a parameterless span plus explicit
tagging (below), because the tag extensions already know how to render the
domain concepts. Follow the style of the area you are editing.

## Step 2 — Take the activity source

The registration is keyed by `Cratis.Chronicle.Concepts.WellKnown.MeterName`
(the constant is `"Cratis.Chronicle"`), so kernel classes ask for the keyed
service:

```csharp
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;

public class <ClassName>(
    <OtherDependencies>,
    [FromKeyedServices(WellKnown.MeterName)] IActivitySource<<ClassName>> activitySource)
```

For a class constructed manually — a service factory, for instance — resolve it
the same way:

```csharp
serviceProvider.GetRequiredKeyedService<IActivitySource<<ClassName>>>(WellKnown.MeterName)
```

### Why no per-type registration

`ChronicleMetersExtensions.AddChronicleMeters()` calls
`services.AddNamedMeter(WellKnown.MeterName)` and
`services.AddNamedActivitySource(WellKnown.MeterName)` — both from
`Cratis.Fundamentals` — and then replaces the open-generic keyed
`IActivitySource<>` registration with Chronicle's own `KeyedActivitySource<>`.
Because the registration is **open generic**, any new `T` injected into a keyed
class resolves automatically. Adding a traced class is a one-file change plus the
constructor parameter.

`IActivitySource<T>` itself is minimal:

```csharp
public interface IActivitySource<T>
{
    System.Diagnostics.ActivitySource ActualSource { get; }
}

public interface IActivityScope<T> : IDisposable
{
    System.Diagnostics.Activity? Activity { get; }
}
```

## Step 3 — Open the scope and tag it

```csharp
using var span = activitySource.<OperationName>();
span?.Activity?.Tag(<domainConcept>);
```

**Always use the null-conditional `span?.Activity?.Tag(...)`.** The scope's
`Activity` is null when no listener is attached, which is the normal case in
specifications.

Record failure on the activity rather than losing it:

```csharp
using var span = activitySource.<OperationName>();
span?.Activity?.Tag(<domainConcept>);
try
{
    // work
}
catch (Exception ex)
{
    span?.Activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    throw;
}
```

### The tag extensions

`Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing.TagExtensions` provides
`Tag` overloads on `Activity` for the domain concepts:

| Overload | Tags |
| --- | --- |
| `Tag(EventStoreName)` | event store name |
| `Tag(EventStoreNamespaceName)` | namespace |
| `Tag(EventSequenceId)` | event sequence id |
| `Tag(EventType)` | event type id |
| `Tag(EventSourceType, EventSourceId)` | event source type and id |
| `Tag(ObserverId)` | observer id |
| `Tag(ObserverType)` | observer type |
| `Tag(ConnectionId)` | connection id |
| `Tag(ObserverKey)` | observer id, event sequence, namespace, event store |
| `Tag(ConnectedObserverKey, ObserverType?)` | the above plus connection id and type |

Each returns the `Activity`, so calls chain. Add a new overload here rather than
setting a raw tag name at the call site — that is what keeps the tag vocabulary
consistent across the kernel.

Never put an event payload, a read-model value, or anything else that may carry
personal data into a tag. Tags carry identifiers and types.

## Step 4 — Update the specifications

**Never substitute `IActivitySource<T>`.** The generated extension reads
`ActualSource`, which a substitute returns as null, and the span call then throws.
Supply a real one:

```csharp
new ActivitySource<<ClassName>>()
```

For a grain under the Orleans test kit:

```csharp
_silo.AddKeyedService<IActivitySource<<ClassName>>>(
    WellKnown.MeterName,
    new ActivitySource<<ClassName>>());
```

For a manually constructed class, pass `new ActivitySource<<ClassName>>()` in the
constructor call. Add `using Cratis.Traces;` where needed.

## Verify

- `<ClassName>Traces.cs` exists beside `<ClassName>.cs`, with the standard
  license header and the three suppressions.
- The traces class is `internal static partial`; each span method is
  `internal static partial`, returns `IActivityScope<TClass>`, and extends
  `IActivitySource<TClass>`.
- Span names use the prefix and casing of the side they belong to, and the
  `ActivityKind` matches the call's nature.
- The class takes `IActivitySource<T>`, keyed with `WellKnown.MeterName` where
  the surrounding code is keyed.
- No per-type registration was added — the open-generic registration covers it.
- Every span use is null-conditional, and failures set the activity status.
- Tags come from the tag extensions, and no payload data is tagged.
- Specifications use `new ActivitySource<T>()`, never a substitute.
- The solution builds with zero warnings and the affected specifications pass.
