---
agent: agent
description: Create a strongly-typed Concept (ConceptAs<T>) for a primitive domain value.
---

# Add a Concept

I need to create a **strongly-typed Concept** to wrap a primitive domain value.

## Inputs

- **Concept name** — e.g. `ProjectId`, `AuthorName`, `InvoiceNumber`
- **Underlying primitive type** — `Guid`, `string`, `int`, `long`, `decimal`, etc.
- **Is this an event-source ID?** — If yes, an `EventSourceId` implicit conversion is needed
- **Does it need a `New()` factory?** — Typically yes for ID types backed by `Guid`
- **Folder** — where in `Features/` or `Engine/` to place it (matches domain context, NOT a dedicated `Concepts/` folder)

## Instructions

Follow `.github/instructions/concepts.instructions.md` exactly.

### Template

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace <NamespaceRoot>.<Feature>;

/// <summary>
/// Represents the identity of a <description>.
/// </summary>
/// <param name="Value">The underlying <type> value.</param>
public record <ConceptName>(<UnderlyingType> Value) : ConceptAs<<UnderlyingType>>(Value)
{
    /// <summary>
    /// Represents an unset or empty <ConceptName>.
    /// </summary>
    public static readonly <ConceptName> NotSet = new(<EmptyValue>);

    /// <summary>
    /// Implicitly converts a <UnderlyingType> to a <ConceptName>.
    /// </summary>
    public static implicit operator <ConceptName>(<UnderlyingType> value) => new(value);
}
```

### When the concept is an event-source ID (add after the implicit from primitive)

```csharp
    /// <summary>
    /// Implicitly converts a <ConceptName> to an <see cref="EventSourceId"/>.
    /// </summary>
    public static implicit operator EventSourceId(<ConceptName> id) => new(id.Value.ToString());
```

### When a `New()` factory is needed (Guid-backed IDs)

```csharp
    /// <summary>
    /// Creates a new <ConceptName> with a unique value.
    /// </summary>
    public static <ConceptName> New() => new(Guid.NewGuid());
```

### Empty values by type

| Underlying type | Recommended empty value |
|-----------------|------------------------|
| `Guid`          | `Guid.Empty`           |
| `string`        | `string.Empty`         |
| `int`           | `0`                    |
| `long`          | `0L`                   |

## Placement rules

- Do NOT create a `Concepts/` folder.
- Place the file in the folder that owns the concept semantically.
  - `ProjectId` lives in `Features/Projects/` or the highest-level folder that uses it.
  - Shared cross-feature concepts live in the project root source folder.

## Checklist

- [ ] Inherits `ConceptAs<T>` (not a plain `record` with a value)
- [ ] Has a `static readonly NotSet` (or `Empty`) sentinel
- [ ] Has an implicit conversion **from** the primitive type
- [ ] Has an implicit conversion **to** `EventSourceId` if used as event-source key
- [ ] Has a `New()` factory if `Guid`-backed
- [ ] Copyright header present
- [ ] `dotnet build` passes with zero errors or warnings
