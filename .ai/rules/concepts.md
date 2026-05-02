---
applyTo: "**/*.cs"
---

# Concepts — Strongly-Typed Domain Values

## Why

A `Guid` is a `Guid` is a `Guid` — the compiler can't tell you if you accidentally passed a `UserId` where an `AuthorId` was expected. Both are `Guid`, both compile, and the bug hides until production.

`ConceptAs<T>` wraps every domain value in a named type. The compiler now enforces that an `AuthorId` is not a `UserId`, method signatures become self-documenting, and code review becomes about logic rather than "is this the right ID?"

Never use raw primitives (`string`, `int`, `Guid`) in domain models, commands, events, or queries.

## Rules

- Inherit from `ConceptAs<T>` as a positional `record` — this gives you value equality and immutability for free.
- `ConceptAs<T>` already provides an implicit conversion **from** `T` **to** the concept.
- Always add an implicit conversion operator **from** the concept **to** `T` for easy extraction.
- Add a `static readonly` sentinel value instead of using `null` — sentinels make "no value" explicit and avoid nullable reference type noise:
  - `Guid` → `NotSet` backed by `Guid.Empty`
  - `string` → `NotSet` backed by `string.Empty`
  - `int` / `long` → `NotSet` backed by `0` / `0L`
- For `Guid`-backed identity concepts, add a `static New()` factory method — it reads better than `new AuthorId(Guid.NewGuid())`.
- If the concept is used as a Chronicle event source key, add an implicit conversion to `EventSourceId` — this lets Chronicle resolve the key automatically.
- Place concepts in the **feature folder they belong to** — never create a standalone `Concepts/` folder. Concepts shared between slices go in the feature root; shared between features go in `Features/` root.
- One concept per file, named after the concept (e.g. `AuthorId.cs`, `AuthorName.cs`).

## Examples

**Guid identity concept (full canonical pattern):**

```csharp
public record AuthorId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly AuthorId NotSet = new(Guid.Empty);

    public static implicit operator Guid(AuthorId id) => id.Value;
    public static implicit operator AuthorId(Guid value) => new(value);
    public static implicit operator EventSourceId(AuthorId id) => new(id.Value.ToString());

    public static AuthorId New() => new(Guid.NewGuid());
}
```

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
