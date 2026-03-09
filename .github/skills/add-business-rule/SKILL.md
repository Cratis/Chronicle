---
name: add-business-rule
description: Use this skill when asked to add a validation rule, business rule, or uniqueness constraint to an existing command in a Cratis-based project.
---

Add a business rule or event-store constraint to an existing command.

## Choose the right mechanism

| Scenario                                           | Use                                           |
|----------------------------------------------------|-----------------------------------------------|
| Uniqueness that must survive concurrent writes     | `IConstraint`                                 |
| Rule requiring Chronicle event-sourced state       | ReadModel as `Handle()` parameter (DCB)       |
| Simple sync invariant (format, range, required)    | `CommandValidator<T>`                         |

## Business Rules via DCB (ReadModel as argument)

Use when the rule depends on **Chronicle event-sourced state** (e.g. current count, accumulated value).
This is the DCB (Dynamic Consistency Boundary) pattern: add a **read model parameter** to the `Handle()` method.
The framework fetches and injects the current read model snapshot before `Handle()` runs.

The read model must already exist in the slice (decorated with `[ReadModel]` and a model-bound projection).
If it doesn't, add it first — see the `new-vertical-slice` skill.

```csharp
[Command]
public record <Command>(<KeyProperty> <Key>, ...)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="readModelParam">The current state.</param>
    /// <returns>The resulting event.</returns>
    public <Event> Handle(<ReadModel> <readModelParam>)
    {
        if (<readModelParam>.<StateProperty> <violatesRule>)
            throw new <ViolationException>(<meaningful message>);

        return new <Event>(...);
    }
}
```

**Example — limit items in cart to 3:**

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

**Key rules:**
- The read model parameter type must match a `[ReadModel]`-decorated type in the same feature.
- The framework resolves the instance using the same event-source key as the command.
- Throw a **custom exception** (never a built-in one) to signal violation — the framework converts it to an error result.
- One parameter per logical read model; if you need multiple reads, use multiple parameters.

## Event-Store Constraints (`IConstraint`)

Enforced by Chronicle at append time. Use for uniqueness that must hold across concurrent writes.

```csharp
public class Unique<PropertyName> : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(unique =>
            unique
                .On<<EventType>>(e => e.<UniqueProperty>)
                .RemovedWith<<RemovedEventType>>());  // omit if there is no remove event
}
```

**Example — unique project name:**

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

Constraints are discovered and enforced automatically — no registration or attribute on the command needed.

## Simple validators (`CommandValidator<T>`)

For format, range, and required-field rules that do not need event-sourced state — evaluated before `Handle()` runs:

```csharp
public class RegisterAuthorValidator : CommandValidator<RegisterAuthor>
{
    public RegisterAuthorValidator()
    {
        RuleFor(c => c.Name).NotEmpty().WithMessage("Name is required.");
        RuleFor(c => c.Name).MaximumLength(100).WithMessage("Name must be 100 characters or fewer.");
    }
}
```

Validators are discovered automatically — no registration needed. If any rule fails, Chronicle returns an error result and `Handle()` is never called.

## After adding

1. Add a spec for the failure case — see `write-specs` skill
2. Run `dotnet build` and `dotnet test`
3. Fix all failures before completing
