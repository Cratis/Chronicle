---
name: cratis-fundamentals-concept
description: Create strongly typed Cratis domain values with ConceptAs<T> and Chronicle event-source identities with EventSourceId<T>. Use when a C# domain value has meaning beyond its primitive or when an identity is actually used as a Chronicle event-source/stream ID. Do not use for enums, DTO-only transport values, arbitrary non-stream entity IDs, or event schema migration.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-fundamentals-concept/SKILL.md -->

# Cratis domain concepts and event-source identities

Replace a primitive only when the domain gives it distinct meaning. Keep value
concepts and Chronicle stream identities separate.

## Verified product sources

This skill is verified against these exact public releases:

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Fundamentals` | `7.18.1` | `Cratis.Concepts.ConceptAs<T>` |
| `Cratis.Chronicle` | `16.38.1` | `Cratis.Chronicle.Events.EventSourceId` and `EventSourceId<T>` |

Reverify product sources before claiming support for another version.

## Choose the type

- Derive a name, amount, code, number, or non-stream entity ID from
  `ConceptAs<T>`.
- Derive an identity from `EventSourceId<T>` only when that value is actually
  passed to Chronicle as the event-source/stream ID.
- Do not use `ConceptAs<Guid>` for a Chronicle stream identity.
- Do not use `EventSourceId<T>` merely because a value is called an ID.
- Do not wrap an enum. An enum already expresses a closed domain concept.
- Keep DTO-only transport values primitive unless the domain type belongs in the
  public contract.

Both generic bases require an underlying type that implements `IComparable`.

## Create a value concept

A value concept contains exactly one wrapped value. Do not add extra properties;
Fundamentals converters assume the concept is a single-value type and additional
state can be lost during serialization.

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace <NamespaceRoot>.<Feature>;

/// <summary>
/// Represents the <description>.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record <ConceptName>(<ComparableUnderlyingType> Value) :
    ConceptAs<<ComparableUnderlyingType>>(Value);
```

`ConceptAs<T>` supplies implicit conversion from the concept to `T`. Add the
reverse conversion only when it improves the domain API:

```csharp
public static implicit operator <ConceptName>(<ComparableUnderlyingType> value) =>
    new(value);
```

Primitive-to-concept conversion is optional; it is not a Fundamentals
requirement.

### Absence and sentinels

`ConceptAs<T>` rejects a null wrapped value. Represent absence with a nullable
concept reference such as `<ConceptName>?` when absence is valid.

A `NotSet` or `Empty` value is optional domain policy. Add one only when the
chosen primitive value is impossible or explicitly reserved in that domain.
Do not assume `string.Empty`, `0`, or `Guid.Empty` is universally invalid.

## Create a Guid-backed Chronicle stream identity

Use this shape only for an identity actually supplied to Chronicle append/read
operations as the event-source ID.

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace <NamespaceRoot>.<Feature>;

/// <summary>
/// Represents the event-source identity of a <description>.
/// </summary>
/// <param name="Value">The underlying Guid value.</param>
public record <ConceptName>(Guid Value) : EventSourceId<Guid>(Value)
{
    /// <summary>
    /// Creates a new <ConceptName>.
    /// </summary>
    /// <returns>A new <ConceptName>.</returns>
    public static <ConceptName> New() => new(Guid.NewGuid());

    /// <summary>
    /// Converts a Guid to a <ConceptName>.
    /// </summary>
    public static implicit operator <ConceptName>(Guid value) => new(value);
}
```

`New()` and the primitive-to-derived conversion are conveniences on this domain
type. `EventSourceId<T>` does not construct an arbitrary derived identity for
you.

## Create a non-Guid Chronicle stream identity

Use a factory only when the domain has an authoritative way to create the
underlying value.

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace <NamespaceRoot>.<Feature>;

/// <summary>
/// Represents the event-source identity of a <description>.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record <ConceptName>(<ComparableUnderlyingType> Value) :
    EventSourceId<<ComparableUnderlyingType>>(Value)
{
    /// <summary>
    /// Converts the underlying value to a <ConceptName>.
    /// </summary>
    public static implicit operator <ConceptName>(<ComparableUnderlyingType> value) =>
        new(value);
}
```

The exact `EventSourceId<T>` base supports conversions among `T`, string,
untyped `EventSourceId`, and `EventSourceId<T>`. Those operators do not create
your derived `<ConceptName>` from `T`, string, or untyped `EventSourceId`.
Declare only the derived-type conversions your domain API needs.

String and Guid are the safest round-trip primitives. Chronicle also supports
constructible `ConceptAs<string>` and `ConceptAs<Guid>` values. Other comparable
values rely on `Convert.ChangeType`; verify round-trip behavior before using
them as stream IDs.

### Unspecified and sensitive identities

`EventSourceId.Unspecified` belongs to the untyped string-backed ID.
`Guid.Empty`, `0`, `0L`, and similar typed values become real, specified stream
IDs after conversion; they are not Chronicle's unspecified value. Treat any
sentinel on a typed identity as explicit domain policy, not framework behavior.

Never use a sensitive natural identifier directly as an event-source ID.
Chronicle cannot encrypt event-source IDs. Use a random surrogate stream ID and
store the sensitive value separately under the approved compliance model.

## Use the identity with Chronicle

Pass the typed identity as the append/read event-source ID. Merely declaring an
`EventSourceId<T>` property does not select the event stream.

Do not add `[Key]` or `[Subject]` to an `EventSourceId<T>`-derived member;
Chronicle analyzer `CHR0026` reports that misuse. Do not add `[PII]` to an
event-source ID; analyzer `CHR0034` rejects it.

## Placement is an application convention

In a Cratis application, place the concept with the feature that owns its
meaning rather than in a generic `Concepts/` folder. Put genuinely cross-feature
concepts in `Common/`. Do not introduce a top-level `Features/` wrapper.

This placement is a Cratis application convention, not a Fundamentals or
Chronicle API requirement. Framework and client repositories follow their own
repository structure.

## Verify

- `ConceptAs<T>` and `EventSourceId<T>` use an `IComparable` underlying type.
- A concept contains exactly one wrapped value and no extra properties.
- Enums remain enums.
- Null absence uses a nullable concept reference rather than a null wrapped
  value.
- Primitive-to-derived conversions and sentinels exist only when justified by
  the domain.
- An `EventSourceId<T>` type represents a real Chronicle stream identity.
- The typed identity is passed explicitly to Chronicle operations.
- No `[Key]`, `[Subject]`, or `[PII]` attribute is placed on the stream identity.
- Sensitive natural identifiers use a surrogate stream ID.
- The file carries the repository license header.
- The project builds and its relevant specifications pass against the verified
  package versions.
