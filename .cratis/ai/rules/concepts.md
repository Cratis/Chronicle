---
applyTo: "**/*.cs"
paths:
  - "**/*.cs"
---
<!-- cratis-ai-managed: rules/concepts.md -->

# Concepts — Strongly Typed Domain Values

## Why

A primitive such as `Guid` or `string` does not tell the compiler whether a
value is an `AuthorId`, `UserId`, or `InvoiceNumber`. A Cratis concept gives a
meaningful value its own type and keeps that meaning in method signatures,
serialization, validation, events, and read models.

Use a concept for a real domain value, not mechanically for every primitive in a
DTO or framework API. An enum already represents a closed domain concept and
does not need a `ConceptAs<T>` wrapper.

## Product contracts

- A value concept derives from `ConceptAs<T>` in `Cratis.Fundamentals`.
- A value actually used as a Chronicle event-source/stream identity derives from
  `EventSourceId<T>` in `Cratis.Chronicle`.
- An arbitrary entity ID that does not identify a Chronicle stream remains a
  value concept; do not derive it from `EventSourceId<T>` merely because its name
  ends in `Id`.
- Both generic bases require an underlying type implementing `IComparable`.

## Value concept rules

- Use a positional record containing exactly one wrapped value. Fundamentals
  serialization assumes a single-value concept; do not add extra properties.
- `ConceptAs<T>` supplies concept → `T` conversion.
- Add `T` → derived concept conversion only when it improves the domain API. It
  is optional, not a framework requirement.
- `ConceptAs<T>` rejects a null wrapped value. Use a nullable concept reference
  such as `AuthorName?` when absence is valid.
- `NotSet` or `Empty` is optional domain policy. Add one only when the backing
  value is impossible or explicitly reserved in that domain. Do not assume
  empty string, zero, or `Guid.Empty` is universally invalid.
- Mark a concept that holds **personal data** `[PII]` and one that holds a
  **secret** `[NotAudited]`. Both markings travel with the type, so marking it
  once covers every command and event that uses it - which is the point of
  having the concept. A command's property values are written to the causation
  chain of every event it appends, so an unmarked `ApiKey` or `AccessToken`
  concept reaches the event log in the clear and stays there. The two are not
  interchangeable: `[PII]` also encrypts and enrolls the value in erasure, which
  is wrong for a password, and `[NotAudited]` does nothing for an erasure
  request, which is wrong for a name.

```csharp
public record AuthorName(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator AuthorName(string value) => new(value);
}
```

## Chronicle stream identity rules

```csharp
public record AuthorId(Guid Value) : EventSourceId<Guid>(Value)
{
    public static AuthorId New() => new(Guid.NewGuid());
    public static implicit operator AuthorId(Guid value) => new(value);
}
```

- `New()`, primitive → derived ID conversion, and sentinels are optional domain
  conveniences; `EventSourceId<T>` does not create an arbitrary derived type.
- The exact `EventSourceId<T>` base supports conversions among its underlying
  `T`, string, typed ID, and untyped `EventSourceId`, but those operators do not
  construct your derived domain record from every source form.
- String and Guid are the safest round-trip primitives. Other comparable values
  rely on Chronicle conversion behavior and require focused verification.
- Pass the identity explicitly to Chronicle append/read operations. Declaring an
  `EventSourceId<T>` property does not select the stream.
- Do not put `[Key]` or `[Subject]` on an `EventSourceId<T>`-derived member;
  Chronicle analyzer `CHR0026` reports it.
- Do not put `[PII]` on an event-source ID; analyzer `CHR0034` rejects it.
  Sensitive natural identifiers use a random surrogate stream ID and a separate
  compliance-managed value.
- `EventSourceId.Unspecified` is the untyped string-backed sentinel. Typed empty
  or zero values convert to real specified stream IDs and are not equivalent to
  `Unspecified`.

## Application placement convention

In a Cratis application, place a concept with the feature/module that owns its
meaning rather than in a generic `Concepts/` folder. Put genuinely cross-feature
concepts in `Common/`. Do not introduce a top-level `Features/` wrapper.

This is a Cratis application convention, not a Fundamentals or Chronicle API
contract. Framework and client repositories follow their own structure.

## Promote a value deliberately

Promote a value when its meaning or cross-cutting characteristics must travel
with it—for example validation, compliance classification, or a domain-specific
format. Reuse an existing shared concept when it already owns that meaning.

## Call-site guidance

Use the constructors, conversions, factories, or sentinels the domain type
actually provides. Do not require `NotSet`, `New()`, or a reverse conversion on
every concept. If a sentinel exists, reference the named sentinel rather than
reconstructing its backing primitive.

## Geospatial values

Use GeoJSON types from `Cratis.Geospatial`: `Point` for a location,
`LineString` for a route, and `Polygon` for an area. Do not use the removed
experimental `Coordinate` type. Model geospatial absence according to the
owning domain contract; do not invent a nullable event payload that conflicts
with Chronicle event rules.
