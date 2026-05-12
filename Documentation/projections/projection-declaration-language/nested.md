# Nested Objects

Nested objects define a single nullable child object within a projection. Unlike [children](children.md), which manage collections of items, `nested` targets a scalar property that holds one optional object — set when a specific event occurs and cleared (set to null) when another event occurs.

## Basic Syntax

```pdl
nested {PropertyName}
  from {SetEvent}
  clear with {ClearEvent}
```

## Simple Example

```pdl
projection Slice => SliceReadModel
  from SliceCreated
    Name = name

  nested command
    from CommandSetForSlice
      Name = commandName
      Schema = schema
    clear with CommandClearedForSlice
```

This creates a `command` property on `SliceReadModel` that is populated from `CommandSetForSlice` and set back to null when `CommandClearedForSlice` is appended.

## From Event

The `from` block inside `nested` works exactly like a top-level `from` block — you can map properties explicitly or rely on AutoMap:

```pdl
nested command
  from CommandSetForSlice
    Name = commandName
    Schema = schema
    Rules = validationRules
```

## Clear With

Use `clear with` to specify which event nulls the nested object:

```pdl
nested command
  from CommandSetForSlice
  clear with CommandClearedForSlice
```

When a `CommandClearedForSlice` event is appended, the `command` property on the parent is set to `null`.

## Multiple From Events

The nested object can be updated by more than one event:

```pdl
nested command
  from CommandSetForSlice
    Name = commandName
    Schema = schema

  from CommandRenamed
    Name = newName

  from CommandSchemaUpdated
    Schema = updatedSchema

  clear with CommandClearedForSlice
```

Each `from` can map a different subset of properties. Subsequent events update only the properties they map — they do not replace the entire nested object.

## With AutoMap

Apply AutoMap to avoid explicit property mappings when names match:

```pdl
nested command
  automap

  from CommandSetForSlice
  from CommandUpdated
  clear with CommandClearedForSlice
```

## Every Block in Nested

Use an `every` block inside `nested` to apply mappings across all events that affect the nested object:

```pdl
nested command
  every
    LastModified = $eventContext.occurred

  from CommandSetForSlice
    Name = commandName
    Schema = schema

  from CommandUpdated
    Schema = updatedSchema

  clear with CommandClearedForSlice
```

`LastModified` is updated on every event that touches the `command` property.

## Nested Within Nested

A nested object can itself contain another nested object:

```pdl
nested command
  from CommandSetForSlice
    Name = commandName

  nested validation
    from ValidationConfigured
      Rules = rules
    clear with ValidationRemoved

  clear with CommandClearedForSlice
```

## Nested Within Children

A `nested` block can appear inside a `children` block:

```pdl
children tasks id taskId
  from TaskAdded key taskId
    parent projectId
    Title = title

  nested assignee
    from TaskAssigned
      Name = assigneeName
      Email = assigneeEmail
    clear with TaskUnassigned
```

## Read Model Structure

The nested property maps to a nullable property on the parent read model:

```csharp
public class SliceReadModel
{
    public string Name { get; set; }
    public CommandItem? Command { get; set; }  // nullable — null until set
}

public class CommandItem
{
    public string Name { get; set; }
    public string Schema { get; set; }
}
```

## Examples

### Slice with Optional Command

```pdl
projection Slice => SliceReadModel
  from SliceCreated
    Name = name

  nested command
    automap

    from CommandSetForSlice
    from CommandRenamed
      Name = newName

    clear with CommandClearedForSlice
```

### Employee with Optional Active Contract

```pdl
projection Employee => EmployeeReadModel
  from EmployeeHired
    Name = name
    Department = department

  nested activeContract
    from ContractStarted
      ContractId = contractId
      StartDate = startDate
      EndDate = endDate
      Type = contractType

    from ContractExtended
      EndDate = newEndDate

    clear with ContractEnded
```

### Product with Optional Promotion

```pdl
projection Product => ProductReadModel
  from ProductListed
    Name = name
    BasePrice = price

  nested promotion
    from PromotionApplied
      Label = promotionName
      DiscountPercent = discount
      ValidUntil = expiresAt

    clear with PromotionRemoved
```

## Best Practices

1. **Nullable property**: Always define the nested read model property as nullable (`T?`) — it starts null and is only populated when the `from` event fires
2. **Single responsibility**: Model the nested type around one coherent concept; if it needs its own lifecycle or collection behaviour, use [children](children.md) instead
3. **Clear semantics**: Provide a `clear with` event only when the nested object has a meaningful absent state — do not use it to model soft-deletion of the parent
4. **AutoMap selectively**: Use AutoMap when event property names align with the read model; use explicit mappings when the event contains extra fields you do not want on the nested object

## See Also

- [Children](children.md) — collections of items managed independently within a parent
- [From Event](from-event.md) — how event mapping works
- [Auto-Map](auto-map.md) — automatic property mapping
- [Removal](removal.md) — removing root read models
