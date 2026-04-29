# Event Source Id

An event source identifier (`EventSourceId`) is the key that groups related events together within an event sequence. Every event you append must carry an event source identifier — it is how Chronicle knows which instance of an entity the event belongs to.

## How the server works

Internally, Chronicle represents all event source identifiers as plain strings. This keeps the server and storage layer simple and schema-independent: the server does not need to know whether your identifier is a `Guid`, an integer, or a domain concept. Any type you use on the client side is serialized to its string representation before it reaches the server.

## The `EventSourceId` type

The `EventSourceId` record is the primary type used throughout the Chronicle API. It wraps a `string` value and provides implicit conversions from both `string` and `Guid`, so you can pass either directly wherever an `EventSourceId` is expected.

```csharp
using Cratis.Chronicle.Events;

// From a Guid — common for aggregate-style identifiers
EventSourceId id = Guid.NewGuid();

// From a string — useful for natural keys
EventSourceId id = "order-42";

// Generate a new random identifier
EventSourceId id = EventSourceId.New();
```

The `Unspecified` sentinel value signals the absence of a meaningful identifier and corresponds to an empty string. Use it when an identifier is structurally required but contextually irrelevant.

## The generic `EventSourceId<T>`

The generic `EventSourceId<T>` type lets you carry a strongly-typed value through the client-side API while remaining wire-compatible with the plain `EventSourceId` that the server expects.

```csharp
using Cratis.Chronicle.Events;

// A strongly-typed domain concept
public record CustomerId(Guid Value) : EventSourceId<Guid>(Value)
{
    public static CustomerId New() => new(Guid.NewGuid());
}

// Use the typed identifier with the event log
var customerId = CustomerId.New();
await eventLog.Append(customerId, new OrderPlaced(customerId, total));
```

Because `EventSourceId<T>` carries an implicit conversion to `EventSourceId`, you can pass a typed identifier anywhere the untyped form is expected — including directly to `IEventLog.Append` — without any manual conversion. The typed form is most useful when you want the identifier to carry its concrete type through multiple layers so that callers always see the domain concept rather than a plain string.

## Frontend proxy generation

When you use `EventSourceId<T>` in a command or query parameter, the Arc ProxyGenerator reads the underlying type `T` and emits a TypeScript proxy that reflects the concrete type rather than a plain string. This gives you a fully typed frontend experience that matches the domain model on the server.

See <xref:Arc.Chronicle.ProxyGeneration> for details on how proxy generation works and how to configure it.
