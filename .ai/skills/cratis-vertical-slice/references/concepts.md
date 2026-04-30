# Concepts — Reference

## What is a Concept?

A `ConceptAs<T>` wraps a primitive (`Guid`, `string`, `int`, etc.) in a named domain type. The compiler enforces that you cannot pass a `UserId` where an `AuthorId` was expected — both are `Guid` underneath, but they are distinct types.

**Never use raw primitives in domain models, commands, events, or queries.**

---

## Full canonical pattern — Guid identity

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

Include the `EventSourceId` implicit conversion on every **identity** concept that is used as a Chronicle event source key.

---

## String value concept

```csharp
public record AuthorName(string Value) : ConceptAs<string>(Value)
{
    public static readonly AuthorName NotSet = new(string.Empty);

    public static implicit operator string(AuthorName name) => name.Value;
    public static implicit operator AuthorName(string value) => new(value);
}
```

---

## Integer value concept

```csharp
public record PageNumber(int Value) : ConceptAs<int>(Value)
{
    public static readonly PageNumber NotSet = new(0);

    public static implicit operator int(PageNumber p) => p.Value;
    public static implicit operator PageNumber(int value) => new(value);
}
```

---

## Rules

| Rule | Detail |
| --- | --- |
| Inherit as `record` | Gives value equality and immutability for free |
| `ConceptAs<T>` provides `T → Concept` implicitly | You only need to add the `Concept → T` direction |
| Always add `NotSet` sentinel | Use `Guid.Empty`, `string.Empty`, or `0` — no `null` |
| Add `New()` on Guid identity types | Reads better than `new AuthorId(Guid.NewGuid())` |
| Add `EventSourceId` conversion on identity keys | Enables Chronicle to auto-resolve the event source |
| One concept per file | Named after the concept, e.g. `AuthorId.cs` |

---

## File placement

| Scope | Location |
| --- | --- |
| Used only within one slice | Inside the slice folder |
| Shared between slices of a feature | Feature root folder (`Features/Authors/AuthorId.cs`) |
| Shared between features | `Features/` root (`Features/TenantId.cs`) |

Never create a standalone `Concepts/` folder — concepts belong near the code that uses them.

---

## In commands and events

```csharp
// Event uses concepts
[EventType]
public record AuthorRegistered(AuthorName Name);

// Command uses concepts
[Command]
public record RegisterAuthor(AuthorName Name)
{
    public (AuthorId, AuthorRegistered) Handle() =>
        (AuthorId.New(), new(Name));
}

// Read model uses concepts
[ReadModel]
[FromEvent<AuthorRegistered>]
public record Author([Key] AuthorId Id, AuthorName Name);
```
