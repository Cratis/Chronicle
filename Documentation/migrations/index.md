# Migrations

Every system evolves. Business rules change, data models improve, and properties get renamed, split, or combined. In event-sourced systems this creates a challenge: events are immutable facts, and the historical record cannot simply be edited. Chronicle solves this with a declarative migration system that lets you evolve event schemas without losing backward compatibility.

## Why migrations?

Events in Chronicle represent things that happened, not the current state. That immutability is a feature — it gives you a reliable audit trail and the ability to replay history. But it also means you need a principled way to handle changes to event schemas over time.

Chronicle's migration system addresses this through **generations**. Each version of an event type is a distinct generation. When you introduce a new generation, you declare how to transform events between that generation and its predecessor in both directions:

- **Upcasting** — transforming an older generation into a newer one (e.g. splitting a `Name` field into `FirstName` and `LastName`)
- **Downcasting** — transforming a newer generation back into an older one (e.g. recombining `FirstName` and `LastName` into `Name`)

## Architecture: Kernel-side migrations

**Migrations are a Kernel concern, not a client concern.** The Chronicle Kernel evaluates and applies migrations regardless of whether a client is connected. This is a fundamental design principle:

- Migrations run during event append inside the Kernel
- The Kernel stores every generation of an event simultaneously
- No client coordination is required for migrations to take effect
- Clients that are offline or on older code versions are unaffected

This means your migration logic is durable. Once a migrator is registered with the Kernel, it operates on every event that passes through, whether it originated from a .NET client, a REST call, or a future integration you haven't written yet.

## Multi-version storage

When an event with generation migrations arrives at the Kernel, it stores **all generations** of that event:

- Appending a generation 1 event with a 1→2 migration stores gen 1 and gen 2
- Appending a generation 2 event with a 1→2 migration stores gen 2 and the downcasted gen 1
- Appending a generation 3 event with 1→2 and 2→3 migrations stores gen 1, gen 2, and gen 3

This multi-version storage is what makes event-sourced systems with migrations genuinely decoupled. Consider two services that share an event type:

- **Service A** is running the latest code and expects generation 2
- **Service B** is running older code and expects generation 1

Both services read the same event and each gets the generation they expect. There is no negotiation, no shared upgrade schedule, no risk of one service breaking another. The Kernel handles the translation, and each consumer reads the generation it understands.

```text
Append gen 1 event
        │
        ▼
  Kernel receives
        │
        ├── Store gen 1  ─── Service B reads gen 1
        │
        └── Upcast to gen 2
                │
                └── Store gen 2  ─── Service A reads gen 2
```

## Migration definitions and JmesPath

Migrations are declared as C# classes on the client and sent to the Kernel as structured definitions during event type registration. The Kernel converts each migration into a [JmesPath](https://jmespath.org) expression — a query language for transforming JSON. Because events are stored as JSON, JmesPath gives the Kernel a language-neutral, declarative way to apply transformations without needing .NET-specific code at runtime.

The supported transformation operations map to well-known JmesPath expressions:

| Operation | Description |
|---|---|
| `$split` | Extract a part of a string by splitting on a separator |
| `$combine` | Concatenate multiple properties into one value |
| `$rename` | Read a value from a property with an old name |
| `$defaultValue` | Provide a literal default for a new property |

## Topics

- [C# client usage](dotnet-client.md)
