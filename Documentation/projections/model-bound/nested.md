# Nested Objects

Model-bound projections support a single nullable child object through the `[Nested]` attribute. Unlike [children collections](children.md), which project events into an array of items, a nested object is a scalar nullable property that is set from events on the nested type and cleared (set to null) by a `[ClearWith<TEvent>]` event.

## Basic Example

Mark a nullable property with `[Nested]`, then decorate the nested type with `[FromEvent<TEvent>]` and optionally `[ClearWith<TEvent>]`:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

public record Slice(
    string Name,

    [Nested]
    CommandItem? Command);   // null until CommandSetForSlice is appended

[FromEvent<CommandSetForSlice>]
[ClearWith<CommandClearedForSlice>]
public record CommandItem(
    string Name,
    string Schema);
```

Events:

```csharp
[EventType]
public record CommandSetForSlice(string Name, string Schema);

[EventType]
public record CommandClearedForSlice;
```

## How it works

1. When `CommandSetForSlice` is appended, a `CommandItem` is created and its properties are auto-mapped from the event
2. Subsequent `CommandSetForSlice` events update the existing `CommandItem` in place
3. When `CommandClearedForSlice` is appended, the `Command` property is set to `null`

## `[Nested]` attribute

Place `[Nested]` on the nullable property (or record constructor parameter) that holds the single child object:

```csharp
public record Parent(
    [Nested]
    ChildType? Child);
```

The referenced type is scanned for `[FromEvent<TEvent>]` and `[ClearWith<TEvent>]` attributes on the type itself and on its properties.

## `[ClearWith<TEvent>]` attribute

Apply `[ClearWith<TEvent>]` at the class level on the nested type to declare the event that nulls the property on the parent:

```csharp
[FromEvent<CommandSetForSlice>]
[ClearWith<CommandClearedForSlice>]   // clears Slice.Command when appended
public record CommandItem(string Name, string Schema);
```

Multiple `[ClearWith<TEvent>]` attributes are allowed when several events should each clear the same nested object:

```csharp
[FromEvent<CommandSetForSlice>]
[ClearWith<CommandClearedForSlice>]
[ClearWith<SliceArchived>]
public record CommandItem(string Name, string Schema);
```

## Multiple from events

Use multiple `[FromEvent<TEvent>]` attributes to update the nested object from several event types. Chronicle auto-maps matching property names from each event:

```csharp
[FromEvent<CommandSetForSlice>]       // sets Name and Schema
[FromEvent<CommandRenamed>]           // updates Name
[FromEvent<CommandSchemaUpdated>]     // updates Schema
[ClearWith<CommandClearedForSlice>]
public record CommandItem(string Name, string Schema);
```

## Explicit property mapping on nested types

When property names on the event differ from those on the nested type, or when you need fine-grained control, add mapping attributes directly to the nested type's properties:

```csharp
[FromEvent<CommandSetForSlice>]
[ClearWith<CommandClearedForSlice>]
public record CommandItem(
    [SetFrom<CommandSetForSlice>(nameof(CommandSetForSlice.CommandName))]
    string Name,

    [SetFrom<CommandSetForSlice>(nameof(CommandSetForSlice.JsonSchema))]
    [SetFrom<CommandSchemaUpdated>(nameof(CommandSchemaUpdated.UpdatedSchema))]
    string Schema);
```

## Auto-mapping behavior

By default, Chronicle auto-maps properties from the event to the nested type when names match (case-insensitive). Add `[NoAutoMap]` to the nested type to disable this and require explicit mappings for every property:

```csharp
[FromEvent<CommandSetForSlice>]
[ClearWith<CommandClearedForSlice>]
[NoAutoMap]
public record CommandItem(
    [SetFrom<CommandSetForSlice>(nameof(CommandSetForSlice.CommandName))]
    string Name,

    [SetFrom<CommandSetForSlice>(nameof(CommandSetForSlice.Schema))]
    string Schema);
```

## Multiple nested objects on the same parent

A parent type can have more than one `[Nested]` property:

```csharp
public record Slice(
    string Name,

    [Nested]
    CommandItem? Command,

    [Nested]
    ValidationConfig? Validation);

[FromEvent<CommandSetForSlice>]
[ClearWith<CommandClearedForSlice>]
public record CommandItem(string Name, string Schema);

[FromEvent<ValidationConfigured>]
[ClearWith<ValidationRemoved>]
public record ValidationConfig(string Rules, bool IsStrict);
```

## Nested within children

The `[Nested]` attribute works recursively inside child types. Add it to a property of the child record just as you would on the parent:

```csharp
public record Project(
    [Key] Guid Id,
    string Name,

    [ChildrenFrom<TaskAdded>(key: nameof(TaskAdded.TaskId))]
    IEnumerable<Task> Tasks);

public record Task(
    [Key] Guid TaskId,
    string Title,

    [Nested]
    Assignee? Assignee);   // null until TaskAssigned is appended

[FromEvent<TaskAssigned>]
[ClearWith<TaskUnassigned>]
public record Assignee(string Name, string Email);
```

## Complete Example

The following example shows a slice read model with a command child object that can be set, updated, and cleared:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

// Events
[EventType]
public record SliceCreated(string Name);

[EventType]
public record CommandSetForSlice(
    CommandItemId CommandItemId,
    string Name,
    string Schema,
    string Rules,
    string StateSchema);

[EventType]
public record SliceCommandRenamed(CommandItemId CommandItemId, string NewName);

[EventType]
public record SliceCommandDefinitionUpdated(
    CommandItemId CommandItemId,
    string Schema,
    string Rules,
    string StateSchema);

[EventType]
public record CommandClearedForSlice;

// Read Models
[FromEvent<SliceCreated>]
public record Slice(
    [Key] SliceId Id,
    string Name,

    [Nested]
    CommandItem? Command);

[FromEvent<CommandSetForSlice>]
[ClearWith<CommandClearedForSlice>]
[FromEvent<SliceCommandRenamed>]
[FromEvent<SliceCommandDefinitionUpdated>]
public record CommandItem(
    CommandItemId Id,
    string Name,
    string Schema,
    string Rules,
    string StateSchema);
```

### Event processing flow

| Event | Effect on `Slice.Command` |
|---|---|
| `SliceCreated` | Creates the `Slice`; `Command` remains `null` |
| `CommandSetForSlice` | Populates `Command` with a new `CommandItem` |
| `SliceCommandRenamed` | Updates `Command.Name` in place |
| `SliceCommandDefinitionUpdated` | Updates `Schema`, `Rules`, `StateSchema` in place |
| `CommandClearedForSlice` | Sets `Command` to `null` |

## What works on nested types

All standard model-bound projection attributes are supported on nested types and their properties:

| Attribute | Works on nested type |
|---|---|
| `FromEvent` (class-level) | ✓ |
| `ClearWith` (class-level) | ✓ |
| `SetFrom` | ✓ |
| `AddFrom` / `SubtractFrom` | ✓ |
| `SetFromContext` | ✓ |
| `Increment` / `Decrement` / `Count` | ✓ |
| `Join` | ✓ |
| `Nested` (recursive) | ✓ |
| `ChildrenFrom` (collections within nested) | ✓ |
| `NoAutoMap` | ✓ |

This means you can build arbitrarily nested structures with full projection capabilities at every level.

## Best Practices

1. **Always declare the property as nullable** (`T?`) — the nested object starts null and is only populated when the first `FromEvent` fires
2. **Keep `[ClearWith]` close to `[FromEvent]`** — placing both on the nested type keeps the lifecycle of the object visible in one place
3. **Use AutoMap when names align** — rely on name-matching to avoid repetitive explicit mappings; add `[NoAutoMap]` only when you need full control
4. **One concept per nested type** — if the nested object needs its own collection behavior, use `[ChildrenFrom]` instead

## See Also

- [Children Collections](children.md) — arrays of items managed independently within a parent
- [Basic Mapping](basic-mapping.md) — getting started with model-bound projections
- [Removal](removal.md) — removing root read models on an event
