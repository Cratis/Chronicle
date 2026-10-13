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
- Mark a concept that holds **personal data** `[PII]` alone, and one that holds an
  **operational secret** (API key, token, password, a company's bank account
  number) `[Encrypted]` (`Cratis.Chronicle.ProtectedValues`, Chronicle 19.32.0+)
  **plus** `[NotAudited]`. The markings travel with the type, so marking it
  once covers every command and event that uses it - which is the point of
  having the concept. A command's property values are written to the causation
  chain of every event it appends, so an unmarked `ApiKey` or `AccessToken`
  concept reaches the event log in the clear and stays there. `[Encrypted]`
  protects the value at rest on the event; `[NotAudited]` keeps the command
  input off the causation chain - either alone leaves a gap. `[PII]` already
  encrypts, enrolls the value in erasure and withholds it from the chain, so
  never combine it with `[Encrypted]` (analyzer `CHR0053`). The two kinds are
  not interchangeable: `[PII]` is wrong for a password because it enrolls it in
  erasure, and `[Encrypted]` does not erase a name.

```csharp
public record AuthorName(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator AuthorName(string value) => new(value);
}
```

## Chronicle stream identity rules

```csharp
public record AuthorId(Guid Value) : EventSourceId<Guid>(Value), IGeneratable<AuthorId>
{
    public static AuthorId New() => new(GenerateValue.Uuid());
    public static implicit operator AuthorId(Guid value) => new(value);
}
```

- `New()`, primitive → derived ID conversion, and sentinels are optional domain
  conveniences; `EventSourceId<T>` does not create an arbitrary derived type.
- When the system creates the value, implement `IGeneratable<TSelf>` and use
  `GenerateValue.Uuid()` (or `GenerateValue.UuidV7()` when index locality
  matters), both from `Cratis.Concepts` in Cratis.Fundamentals 7.23.0 or later.
  Never `System.Random`. A client-supplied identifier is an input, not something
  to generate; see the **cratis-fundamentals-concept** skill.
- Arc and Chronicle can resolve an older Cratis.Fundamentals. Check the resolved
  version first; if it is below 7.23.0, add a direct `Cratis.Fundamentals`
  7.23.0 or later reference, or keep the existing `static New()` (without the interface) until you can.
- The exact `EventSourceId<T>` base supports conversions among its underlying
  `T`, string, typed ID, and untyped `EventSourceId`, but those operators do not
  construct your derived domain record from every source form.
- String and Guid remain the safest stream-id primitives. Chronicle v19.50.0
  uses invariant numeric conversion, `"O"` for date/time values and `"c"` for
  TimeSpan (since 19.37.3; `Source/Clients/DotNET/Events/EventSourceId.cs:148-174`).
  Preserve the old untyped id for streams written under legacy date/time or
  decimal-comma culture formats; the new conversion can otherwise open a new stream.
- Pass the identity explicitly to Chronicle append/read operations. Declaring an
  `EventSourceId<T>` property does not select the stream.
- Do not put `[Key]` or `[Subject]` on an `EventSourceId<T>`-derived member;
  Chronicle analyzer `CHR0026` reports it.
- Do not put `[PII]` or `[Encrypted]` on an event-source ID; analyzers `CHR0034`
  and `CHR0052` reject them.
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
with Chronicle event rules. TypeScript imports `Point`, `LineString`, `LinearRing`,
and `Polygon` from `@cratis/fundamentals/geospatial` (verified at 7.23.0).
The wire shape is GeoJSON, for example
`{ "type": "Point", "coordinates": [longitude, latitude] }`: longitude first.
The removal of `Coordinate` in 7.15.1 is a stored-data compatibility break;
plan a migration rather than assuming the old shape deserializes as GeoJSON.
