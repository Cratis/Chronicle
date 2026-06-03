---
name: discover-implementations
description: Use this skill when asked to wire up a type that needs to enumerate every implementation of an interface (handlers, strategies, filters, validators, formatters, providers) in a Cratis-based C# project. Enforces `IInstancesOf<T>` over `IEnumerable<T>` and removes hand-maintained DI registrations.
---

Wire convention-based discovery for a set of implementations behind an abstraction.

## The pattern in one line

Inject `IInstancesOf<TInterface>` from `Cratis.Types`. Mark implementations with `[Singleton]`. Delete any `services.AddSingleton<TInterface, Impl>()` lines that registered them.

## When this skill applies

The consumer of an abstraction needs to iterate, filter, or fan out to **every** registered implementation. Common shapes:

- `*Handlers`, `*Filters`, `*Validators`, `*Formatters`, `*Strategies`, `*Providers`, `*Resolvers` — anything plural that delegates to a set.
- A dispatcher that picks the right implementation by calling `CanHandle(...)` and forwarding to the matching one.
- A composite that fans a single input out to all implementations and aggregates results.

## When it does NOT apply

- The consumer needs **one** implementation chosen at composition time → constructor-inject the concrete interface and let normal DI resolve `IFoo → Foo`.
- The method returns a sequence of values to a caller → `IEnumerable<T>` (or `IReadOnlyList<T>`, etc.) is still the right return type. The rule is only about **injecting** implementations of an abstraction.

## Step 1 — Confirm the implementations are discoverable

`IInstancesOf<T>` discovers types by convention from loaded assemblies. The only requirements:

- Each implementation is a non-abstract `public class`.
- It implements the interface directly (not via another layer that hides it).

No assembly attribute is needed — the Cratis framework's type discovery picks them up automatically.

## Step 2 — Mark implementations as singletons (default)

```csharp
[Singleton]
public class EventResultHandler(IEventTypes eventTypes) : IReactorSideEffectHandler
{
    public bool CanHandle(ReactorContext context, object value) => /* ... */;
    public Task Handle(ReactorContext context, object value) => /* ... */;
}
```

Add `using Cratis;` if `[Singleton]` is unresolved.

Skip `[Singleton]` only when the implementation must be transient — i.e. it holds per-call state that cannot be shared. The convention `IFoo → Foo` still applies for transients; do not register them explicitly.

## Step 3 — Inject `IInstancesOf<T>` in the consumer

```csharp
using Cratis.Types;

[Singleton]
public class ReactorSideEffectHandlers(IInstancesOf<IReactorSideEffectHandler> handlers) : IReactorSideEffectHandlers
{
    public bool CanHandle(ReactorContext context, object value) =>
        handlers.Any(h => h.CanHandle(context, value));

    public Task Handle(ReactorContext context, object value) =>
        handlers.First(h => h.CanHandle(context, value)).Handle(context, value);
}
```

`IInstancesOf<T>` implements `IEnumerable<T>` — LINQ works directly on it. Materialize with `.ToArray()` only if you need a stable snapshot (rare).

## Step 4 — Delete the dead registrations

Find every line in composition roots and service-collection extensions that registered the implementations or the consumer, and remove them:

```csharp
// Delete these — IInstancesOf<T> discovers them, [Singleton] registers them
services.AddSingleton<IReactorSideEffectHandler, EventResultHandler>();
services.AddSingleton<IReactorSideEffectHandler, EventsResultHandler>();
services.AddSingleton<IReactorSideEffectHandlers, ReactorSideEffectHandlers>();
```

Use the codebase search tools to find every reference and verify nothing else relies on these registrations.

## Step 5 — Verify

1. `dotnet build` — zero warnings, zero errors.
2. Run the relevant specs/integration tests for the affected feature.
3. If a `System.MissingMethodException: Cannot dynamically create an instance of type '...'. Reason: Cannot create an instance of an interface.` appears at runtime, an implementation is missing `[Singleton]` or the interface signature changed — re-check Step 2.

## Why this matters

The cost of `services.AddSingleton<TInterface, Impl>()` looks zero at the registration line. The hidden cost shows up later:

- Adding a new implementation in a different folder silently does nothing until someone remembers to register it.
- Removing an implementation leaves a stale registration that fails at startup.
- Spec setups have to duplicate the same registrations to mirror production.
- The composition root grows linearly with the number of implementations — high churn, high merge conflict.

`IInstancesOf<T>` and `[Singleton]` push that knowledge into the implementation itself. Adding or removing an implementation is a single-file change.
