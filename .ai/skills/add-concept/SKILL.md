---
name: add-concept
description: Use this skill when asked to create a strongly-typed domain identifier or value (such as ProjectId, AuthorName, InvoiceNumber) in a Cratis-based project. Produces a ConceptAs<T> record with the correct conversions and sentinel values.
---

Create a strongly-typed Concept that wraps a primitive domain value.

## Never use raw primitives in domain models

Replace `Guid`, `string`, `int`, etc. with a `ConceptAs<T>` record whenever the value has domain meaning.

## Pick the base: value vs identity

- **Value concept** (name, amount, code) → derive from `ConceptAs<T>`.
- **Identity concept** (an entity's event-source id) → derive from `EventSourceId<T>`. **Never** use `ConceptAs<Guid>` for an event-source id — `EventSourceId<T>` already supplies the conversions to/from `T`, to/from `EventSourceId`, and to `string`, so Chronicle resolves the key automatically.

## Value concept template

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace <NamespaceRoot>.<Feature>;

/// <summary>
/// Represents the <description>.
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

## Identity concept template (event-source id)

```csharp
using Cratis.Chronicle.Events;

namespace <NamespaceRoot>.<Feature>;

/// <summary>
/// Represents the identity of a <description>.
/// </summary>
/// <param name="Value">The underlying <type> value.</param>
public record <ConceptName>(<UnderlyingType> Value) : EventSourceId<<UnderlyingType>>(Value)
{
    public static readonly <ConceptName> NotSet = new(<emptyValue>);
    public static <ConceptName> New() => new(Guid.NewGuid());   // when backed by Guid
    public static implicit operator <ConceptName>(<UnderlyingType> value) => new(value);
}
```

Don't redeclare the `EventSourceId` / `T` / `string` conversions — the base provides them.

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
  - `ProjectId` → `Projects/` (the feature folder, directly under the source root — no `Features/` wrapper)
  - Shared cross-feature concepts → `Common/`

## Checklist

- [ ] Inherits `ConceptAs<T>` (value) or `EventSourceId<T>` (identity / event-source id)
- [ ] Has a `static readonly NotSet` (or `Empty`) sentinel
- [ ] Has implicit conversion **from** the primitive
- [ ] Identity concept: does **not** redeclare the `EventSourceId`/`T`/`string` conversions (the base provides them)
- [ ] Has `New()` factory if `Guid`-backed
- [ ] Copyright header present
- [ ] `dotnet build` passes
