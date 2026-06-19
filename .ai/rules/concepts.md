---
applyTo: "**/*.cs"
paths:
  - "**/*.cs"
---

# Concepts — Strongly-Typed Domain Values

## Why

A `Guid` is a `Guid` is a `Guid` — the compiler can't tell you if you accidentally passed a `UserId` where an `AuthorId` was expected. Both are `Guid`, both compile, and the bug hides until production.

`ConceptAs<T>` wraps every domain value in a named type. The compiler now enforces that an `AuthorId` is not a `UserId`, method signatures become self-documenting, and code review becomes about logic rather than "is this the right ID?"

Never use raw primitives (`string`, `int`, `Guid`) in domain models, commands, events, or queries.

## Two kinds of concept

- **Value concepts** (names, amounts, codes) derive from **`ConceptAs<T>`**.
- **Identity concepts** — the event-source id of an entity — derive from **`EventSourceId<T>`** (with the underlying primitive, usually `Guid`). **Never** use `ConceptAs<Guid>` for an event-source identity: `EventSourceId<T>` already gives you conversions to/from `T`, to/from the untyped `EventSourceId`, and to `string`, so Chronicle resolves the key automatically without a hand-written `EventSourceId` operator.

## Rules

- Inherit as a positional `record` — value equality and immutability come for free.
- The base (`ConceptAs<T>` / `EventSourceId<T>`) already provides the implicit conversion **from** the concept **to** `T`. Add your own implicit operator **from** `T` **to** the concept when call-site ergonomics need it — the base does *not* provide that direction.
- Add a `static readonly NotSet` sentinel instead of using `null` — sentinels make "no value" explicit and avoid nullable reference type noise:
  - `Guid` → `NotSet` backed by `Guid.Empty`
  - `string` → `NotSet` backed by `string.Empty`
  - `int` / `long` → `NotSet` backed by `0` / `0L`
- For identity concepts, add a `static New()` factory — it reads better than `new AuthorId(Guid.NewGuid())`.
- Place concepts in the folder they belong to — never a standalone `Concepts/` folder. Slice-specific → slice file; feature-shared → feature folder; module-shared → module folder; app-wide → `Common/`.
- One concept per file, named after the concept (e.g. `AuthorId.cs`, `AuthorName.cs`).

## Examples

**Identity concept — derive from `EventSourceId<T>` (canonical pattern):**

```csharp
public record AuthorId(Guid Value) : EventSourceId<Guid>(Value)
{
    public static readonly AuthorId NotSet = new(Guid.Empty);

    public static AuthorId New() => new(Guid.NewGuid());
    public static implicit operator AuthorId(Guid value) => new(value);
}
```

`EventSourceId<T>` supplies the conversions to `Guid`, `EventSourceId`, and `string` — don't redeclare them.

**String value concept:**

```csharp
public record AuthorName(string Value) : ConceptAs<string>(Value)
{
    public static readonly AuthorName NotSet = new(string.Empty);

    public static implicit operator string(AuthorName name) => name.Value;
    public static implicit operator AuthorName(string value) => new(value);
}
```

**Int value concept:**

```csharp
public record Age(int Value) : ConceptAs<int>(Value)
{
    public static readonly Age NotSet = new(0);

    public static implicit operator int(Age age) => age.Value;
    public static implicit operator Age(int value) => new(value);
}
```

## When to promote a value to its own concept

Promote a value to a dedicated concept when a **cross-cutting characteristic must travel with it** — when the value must consistently carry `[PII]`, a uniqueness constraint, a compliance subject, or a validation rule, encode it as a `ConceptAs<T>` that owns that characteristic. Declared once, it removes the "forgot to annotate it on the next event" gap.

**Guardrail:** don't fragment a shared concept that already owns the characteristic — if one exists, reuse it. (A reducer is *not* a reason to mint a new concept; Chronicle's PII detection is type-aware.)

## Call-site rules

- **Never instantiate an identity with `new <Entity>Id(someVar)` at a call site** — use the implicit `T` → id operator, `NotSet`, or `New()`.
- Optionally pair an identity concept with a `ConceptValidator<T>` that rejects the empty value:

```csharp
public class AuthorIdValidator : ConceptValidator<AuthorId>
{
    public AuthorIdValidator() => RuleFor(_ => _.Value).NotEqual(Guid.Empty);
}
```

(Casting an id to the untyped `EventSourceId` — e.g. `GetInstanceById<T>((EventSourceId)key)` — is a correct, expected idiom; the rule above is about *constructing* ids, not casting them.)

## Geospatial values

For geospatial domain values use the GeoJSON types from `Cratis.Geospatial`: `Point` (a location), `LineString` (a route/path), `Polygon` (an area). Do **not** use the removed experimental `Coordinate` type. Arc, Chronicle, and Fundamentals support these across commands, events, projections, read models, JSON, and generated TypeScript proxies (they serialize as GeoJSON). The normal event rules still apply — model absence with a separate event or a non-geospatial sentinel concept, never `Point?`.
