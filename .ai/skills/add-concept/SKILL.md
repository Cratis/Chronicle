---
name: add-concept
description: Use this skill when asked to create a strongly-typed domain identifier or value (such as ProjectId, AuthorName, InvoiceNumber) in a Cratis-based project. Produces a ConceptAs<T> record with the correct conversions and sentinel values.
---

Create a strongly-typed Concept that wraps a primitive domain value.

## Never use raw primitives in domain models

Replace `Guid`, `string`, `int`, etc. with a `ConceptAs<T>` record whenever the value has domain meaning.

## Template

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
    /// Represents an unset <ConceptName>.
    /// </summary>
    public static readonly <ConceptName> NotSet = new(<emptyValue>);

    /// <summary>
    /// Implicitly converts a <UnderlyingType> to a <ConceptName>.
    /// </summary>
    public static implicit operator <ConceptName>(<UnderlyingType> value) => new(value);
}
```

## Add when backed by Guid

```csharp
    /// <summary>
    /// Creates a new <ConceptName> with a unique value.
    /// </summary>
    /// <returns>A new <ConceptName>.</returns>
    public static <ConceptName> New() => new(Guid.NewGuid());
```

## Add when used as an event-source ID

```csharp
    /// <summary>
    /// Implicitly converts a <ConceptName> to an <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="id">The <ConceptName> to convert.</param>
    public static implicit operator EventSourceId(<ConceptName> id) => new(id.Value.ToString());
```

## Empty/sentinel values

| Underlying type | Use             |
|-----------------|-----------------|
| `Guid`          | `Guid.Empty`    |
| `string`        | `string.Empty`  |
| `int`           | `0`             |
| `long`          | `0L`            |

## Placement rules

- Do NOT create a `Concepts/` folder
- Place the file in the folder that **semantically owns** the concept
  - `ProjectId` → `Features/Projects/`
  - Shared cross-feature concepts → project source root

## Checklist

- [ ] Inherits `ConceptAs<T>`
- [ ] Has a `static readonly NotSet` (or `Empty`) sentinel
- [ ] Has implicit conversion **from** the primitive
- [ ] Has implicit conversion **to** `EventSourceId` if used as event-source key
- [ ] Has `New()` factory if `Guid`-backed
- [ ] Copyright header present
- [ ] `dotnet build` passes
