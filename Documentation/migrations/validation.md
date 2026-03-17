# Generation validation

Chronicle enforces rules about how event type generations and their migration chains must be structured. These rules exist to keep your event history consistent and to prevent silent data loss caused by incomplete migration definitions.

## The rules

When you register an event type at generation 2 or higher, Chronicle validates that:

1. **A generation 1 must exist** — every event type begins at generation 1. There is no such thing as an event that starts at generation 2.
2. **Generations must be sequential with no gaps** — if a type is at generation 3, you must have migrators for 1→2 and 2→3. Jumping directly from 1 to 3 is not allowed.
3. **Every step in the chain must have a migrator** — a migrator must be present for every consecutive generation pair from 1 up to the current generation.

A migration chain that satisfies these rules is called a **valid chain**.

### Valid chain (generation 3)

```text
Gen 1  ──(1→2 migrator)──▶  Gen 2  ──(2→3 migrator)──▶  Gen 3
```

### Invalid chain — missing start

```text
Gen 2  ──(2→3 migrator)──▶  Gen 3        ✗  no 1→2 migrator
```

This throws `MissingFirstGenerationForEventType`.

### Invalid chain — gap

```text
Gen 1                         Gen 3        ✗  no 1→2 or 2→3 migrators
```

This throws `MissingMigrationForEventTypeGeneration`.

### Invalid chain — no migrators at all

```text
[EventType(3)]               ✗  no migrators defined
```

This throws `MissingEventTypeMigrators`.

## Default values for new properties

When a new generation adds a property that did not exist in older events, declare a default value for it in the upcast. Chronicle applies this default to any event stored before the property was introduced.

```csharp
[EventType]
public record AuthorRegisteredV1(string Name);

[EventType(2)]
public record AuthorRegistered(string Name, string Status);

public class AuthorRegisteredMigration : EventTypeMigration<AuthorRegistered, AuthorRegisteredV1>
{
    public override void Upcast(IEventMigrationBuilder<AuthorRegistered, AuthorRegisteredV1> builder) =>
        builder.Properties(pb => pb
            .RenamedFrom(t => t.Name, s => s.Name)
            .DefaultValue(t => t.Status, "active"));

    public override void Downcast(IEventMigrationBuilder<AuthorRegisteredV1, AuthorRegistered> builder) =>
        builder.Properties(pb => pb
            .RenamedFrom(t => t.Name, s => s.Name));
            // Status does not exist in gen 1 — no mapping needed
}
```

`DefaultValue` tells Chronicle: "if this property is absent from the event when upcasting, fill it with this value." Properties that already carry a value are left unchanged.

## Schema immutability

Once a generation's schema is registered with the Kernel, it cannot be changed. If you modify an event record without bumping its generation, Chronicle detects that the schema no longer matches what was originally stored and throws:

```text
Cratis.Chronicle.Services.Events.EventTypeSchemaChanged:
  Event type 'AuthorRegistered' at generation 2 has a schema that differs from the already registered schema.
  Schema changes are not allowed without creating a new generation.
```

The fix is always to introduce a new generation and a corresponding migrator rather than silently mutating an existing one.

## Relaxing validation for development

Strict validation is the default and is always enforced in non-development Kernel builds. During early development — before you have finalised your event schemas — you can disable the checks temporarily.

Set `DisableEventTypeGenerationValidation` in your `ChronicleOptions`:

```csharp
builder.AddCratisChronicle(configureOptions: options =>
{
    options.DisableEventTypeGenerationValidation = true;
});
```

Alternatively, set it in `appsettings.json` under the `Cratis:Chronicle` section:

```json
{
  "Cratis": {
    "Chronicle": {
      "DisableEventTypeGenerationValidation": true
    }
  }
}
```

This value is forwarded to the Kernel as part of the event-type registration request. The Kernel only honours it when running the **development image** — the production image always ignores the flag and validates unconditionally. This makes it impossible to accidentally disable validation in a production deployment even if the client sends `DisableValidation = true`.

## What happens when validation fires

Validation runs on the Kernel during `Register()`, which is called when the client connects to Chronicle. If validation fails the server throws an exception that the client receives as an `RpcException`. This is intentional — failing fast at startup is far better than discovering a broken migration chain at runtime.

| Exception | Condition |
|---|---|
| `MissingEventTypeMigrators` | Event type is at gen ≥ 2 but no migrators are defined |
| `MissingFirstGenerationForEventType` | No migrator covering the path from generation 1 exists |
| `MissingMigrationForEventTypeGeneration` | A specific step in the chain (N → N+1) is missing |
| `InvalidMigrationPropertyForEventType` | A migration references a property that does not exist in the expected generation's schema |
| `EventTypeSchemaChanged` | An existing generation's schema has changed; create a new generation instead |
