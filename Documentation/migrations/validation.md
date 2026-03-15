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

When a new generation adds a property that did not exist in older events, you can declare a default value for that property using the `DefaultValue` operation. Chronicle will apply this default to events that were stored before the new property existed.

```csharp
public class AuthorRegisteredMigration : IEventTypeMigrationFor<AuthorRegistered>
{
    public EventTypeGeneration From => 1;
    public EventTypeGeneration To => 2;

    public void Upcast(IEventMigrationBuilder builder)
    {
        builder.Properties(p =>
        {
            // Copy the existing name field
            p["name"] = p.From("name");

            // New field 'status' — use a default for events that predate it
            p.DefaultValue("status", "active");
        });
    }

    public void Downcast(IEventMigrationBuilder builder)
    {
        builder.Properties(p =>
        {
            p["name"] = p.From("name");
            // 'status' does not exist in gen 1, so nothing to map
        });
    }
}
```

The `DefaultValue(PropertyName targetProperty, object value)` method tells Chronicle: "if this property is absent from the event when upcasting, fill it with this value." Properties that already carry a value are left unchanged.

## Relaxing validation for development

Strict validation is the default and is always enforced in non-development builds. During early development — before you have finalized your event schemas — you can disable the checks temporarily.

Set `DisableEventTypeGenerationValidation` in your `ChronicleOptions`:

```csharp
builder.AddCratisChronicle(configureOptions: options =>
{
    // Only available in DEVELOPMENT builds — no-op in production
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

> **Important:** `DisableEventTypeGenerationValidation` is only honoured when the application is compiled with the `DEVELOPMENT` preprocessor symbol. In all other build configurations the setter is silently ignored and validation always runs. This makes it impossible to accidentally disable validation in a production deployment.

The Chronicle Kernel development build also runs with relaxed defaults, allowing experimental schemas without requiring a complete migration chain.

## What happens when validation fires

Validation runs during `Register()`, which is called when the client connects to Chronicle. If the validation fails, a `MissingEventTypeMigrators`, `MissingFirstGenerationForEventType`, or `MissingMigrationForEventTypeGeneration` exception is thrown before any events are appended. This is intentional — failing fast at startup is far better than discovering a broken migration chain at runtime.

| Exception | Condition |
|---|---|
| `MissingEventTypeMigrators` | Event type is at gen ≥ 2 but no migrators are defined |
| `MissingFirstGenerationForEventType` | No migrator covering the path from generation 1 exists |
| `MissingMigrationForEventTypeGeneration` | A specific step in the chain (N → N+1) is missing |
