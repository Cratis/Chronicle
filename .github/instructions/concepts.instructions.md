---
applyTo: "**/*.cs"
---

# 🧪 How to use Concepts

- Don't use primitives like `string`, `int`, `Guid`, etc. directly in your domain models, commands, events, queries, etc.
- Create a concept for the primitive type that encapsulates the value and provides domain-specific behavior.
- Inherit from the appropriate base class provided by Cratis Concepts, such as `ConceptAs<T>`
- `ConceptAs<T>` has a default conversion from `T` to `ConceptAs<T>`.
- When creating a concept, add a implicit conversion operator from `ConceptAs<T>` to `T` for easy extraction of the underlying value.
- Instead of using `null` values, add a static readonly value representing an empty or default state for the concept with appropriate naming for the context (e.g. `Empty`, `NotSet`).
- Don't add Concepts to its own folder, place it in the folder that makes most sense for the context of the concept.

Example of a concept:

```csharp
public record AuthorId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly AuthorId NotSet = new(Guid.Empty);
    public static implicit operator AuthorId(Guid value) => new(value);

    // If the concept is used as an identifier for an event source, you can add an implicit conversion to EventSourceId
    public static implicit operator EventSourceId(AuthorId id) => new(id.Value.ToString());

    // For convenience, you can add a method to create a new instance with a new unique value - if needed, makes things cleaner when creating new instances
    public static AuthorId New() => new(Guid.NewGuid());
}
```
