---
agent: agent
description: Add a business rule (DCB ReadModel argument) or event-store constraint (IConstraint) to an existing command.
---

# Add a Business Rule or Constraint

I need to enforce a new rule on an existing command in a Cratis-based project.

## Inputs

- **Command** — name of the existing command, e.g. `RegisterProject`
- **Rule type** — one of:
  - **Business rule (DCB)** — add a read model parameter to `Handle()`; the framework fetches and injects the current state before the method runs
  - **Event-store constraint** (`IConstraint`) — enforced by Chronicle at the event-log level; typically uniqueness
- **Rule description** — what must be true for the command to succeed, e.g. "project name must be unique across tenant"

## Business Rules via DCB (ReadModel as argument)

Business rules that depend on Chronicle event-sourced state are expressed by adding a **read model parameter** to the `Handle()` method.
The framework resolves the read model instance (using the same event-source key as the command) and injects it before `Handle()` runs.

```csharp
[Command]
public record AddItemToCart(CartId CartId, ItemId ItemId)
{
    /// <summary>
    /// Adds the item; throws if the cart already holds 3 items.
    /// </summary>
    /// <param name="cart">The current cart summary.</param>
    /// <returns>The <see cref="ItemAddedToCart"/> event.</returns>
    /// <exception cref="CartIsFull">Thrown when the cart already contains the maximum number of items.</exception>
    public ItemAddedToCart Handle(CartSummary cart)
    {
        if (cart.ItemCount >= 3)
            throw new CartIsFullException(CartId);

        return new ItemAddedToCart(ItemId);
    }
}
```

**Rules:**
- The read model parameter type must be a `[ReadModel]`-decorated type in the same feature.
- Throw a **custom exception** (never a built-in one) to signal a violation — the framework converts it to a failed `CommandResult`.
- Multiple read model parameters are allowed if the decision requires more than one projection.

## Event-Store Constraints (`IConstraint`)

Constraints are enforced by Chronicle when the event is appended.
Use when the uniqueness or validity guarantee must survive concurrent writes across multiple instances.

```csharp
public class UniqueProjectName : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(unique =>
            unique
                .On<ProjectRegistered>(e => e.Name)
                .RemovedWith<ProjectRemoved>());
}
```

Constraints are discovered and enforced automatically — no registration required.

### When to use constraints vs business rules

| Scenario | Use |
|----------|-----|
| Uniqueness that must survive concurrent writes | `IConstraint` |
| Rule requiring Chronicle event-sourced state | ReadModel parameter in `Handle()` (DCB) |
| Simple synchronous invariant (format, range) | `CommandValidator<T>` |

## Checklist

- [ ] Rule is placed in the same slice file as the command
- [ ] ReadModel parameter type is a `[ReadModel]`-decorated type in the same feature
- [ ] Custom exception type (never built-in) is thrown to signal a violation
- [ ] Error messages are user-readable and include relevant context
- [ ] Constraints are auto-discovered — no registration needed
- [ ] Spec added for the failure case — see `write-specs.prompt.md`
- [ ] `dotnet build` passes with zero errors
- [ ] `dotnet test` passes with zero failures
