---
name: add-business-rule
description: Use this skill when asked to add a validation rule, business rule, or uniqueness constraint to an existing command in a Cratis-based project.
---

Add a business rule or event-store constraint to an existing command.

## Choose the right mechanism

Pick by what the decision *is* — see [vertical-slices.md](../../rules/vertical-slices.md) "The decision matrix":

| Scenario | Use |
|---|---|
| Reusable value invariant (length, format, range) | `ConceptValidator<T>` on the concept type |
| Command-input / cross-field / pre-handler rule (incl. injected read-model/service checks) | `CommandValidator<TCommand>` with `RuleFor(...)` |
| Handler needs fetched/computed data before it can build the event | `Provide()` — fetch the data; short-circuit with `ValidationResult.Error(...)` if unusable |
| State-dependent rule that must hold **under concurrency** | inject the read model into `Handle()`, return `Result<TEvent, ValidationResult>` |
| Single-event uniqueness (most cases) | `[Unique]` attribute on the event type |
| Multi-event uniqueness or needs `RemovedWith` | `IConstraint` |
| Genuinely exceptional failure (bug, missing infra) | `throw` a custom domain exception |

> **Never throw for normal business rejection.** A thrown exception from `Provide()`/`Handle()` surfaces as `HasExceptions` / HTTP 500 — **not** a validation result. Recoverable, user-facing rejections are validation: return them via a validator, `Provide()`, or `Result<TEvent, ValidationResult>`.

## Business rules via DCB (read model as `Handle()` argument)

Use when the rule depends on **Chronicle event-sourced state** (current count, accumulated value) and must hold **under concurrency**. The framework injects the current read-model snapshot, resolved by the command's event-source id, before `Handle()` runs. Return a `Result<TEvent, ValidationResult>` — success carries the event, failure carries a typed validation error.

The read model must already exist in the slice (`[ReadModel]` + a model-bound projection). If it doesn't, add it first — see the `add-projection` / `cratis-readmodel` skills.

```csharp
[Command]
public record AddItemToCart(CartId CartId, ItemId ItemId)
{
    /// <summary>Adds the item; rejects when the cart already holds the maximum.</summary>
    /// <param name="cart">The current cart summary (injected by event-source id).</param>
    /// <returns>The event on success, or a validation error.</returns>
    public Result<ItemAddedToCart, ValidationResult> Handle(CartSummary cart) =>
        cart.ItemCount >= 3
            ? ValidationResult.Error("A cart can hold at most 3 items.")
            : new ItemAddedToCart(ItemId);
}
```

**Key rules:**
- The read-model parameter type must be a `[ReadModel]` in the same slice/feature; the framework resolves the instance by the command's event-source key. To read a read model keyed differently, use `Provide()` with `IReadModels.GetInstanceById<T>((EventSourceId)key)`.
- Return `Result<TEvent, ValidationResult>` — never throw for the rejection.
- One parameter per logical read model; multiple reads → multiple parameters.

## Pre-handler rules — `CommandValidator<T>`

For rules that don't depend on race-sensitive state, put them in the validator that sits beside the command. Constructor dependencies (read models, services) are injected and resolved by the command's event-source id.

```csharp
public class TransferFundsValidator : CommandValidator<TransferFunds>
{
    public TransferFundsValidator() =>
        RuleFor(c => c.Amount).GreaterThan(0).WithMessage("Amount must be positive.");
}
```

Single-property intrinsic rules belong on `ConceptValidator<T>` instead, so they travel with the value everywhere.

## Event-store constraints — `[Unique]` (preferred)

For uniqueness on a single event type, adorn the event type or one of its properties with `[Unique]`. No separate constraint class is needed; constraints are discovered automatically.

```csharp
// Event-type uniqueness — only one of this event per event source
[EventType]
[Unique(message: "A project with this name already exists.")]
public record ProjectRegistered(ProjectName Name);

// Property uniqueness — the value must be unique across event sources
[EventType]
public record UserRegistered([Unique(name: "UniqueEmail", message: "Email already registered.")] EmailAddress Email, DisplayName Name);

// Release a claimed value on removal — the name must match the [Unique] name exactly
[EventType]
[RemoveConstraint("UniqueEmail")]
public record UserRemoved;
```

Stack multiple `[RemoveConstraint("...")]` attributes on one removal event to release several claimed values at once. To enforce the same property across **multiple** event types, give each its `[Unique(name: "UniqueEmail")]` with the **same name**.

## Event-store constraints — `IConstraint` (advanced)

Use `IConstraint` when uniqueness spans event types with different property names, needs `.IgnoreCasing()`, or a `RemovedWith` event must release it. `Define` is **declarative** — member-access lambdas only, no DI or side effects.

```csharp
public class UniqueProjectName : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(unique =>
            unique
                .On<ProjectRegistered>(e => e.Name)
                .IgnoreCasing()                    // case-insensitive — do NOT lowercase inside the lambda
                .RemovedWith<ProjectRemoved>());   // omit if there is no remove event
}
```

- The `.On<TEvent>(e => e.Prop)` lambda is parsed for **member access only** — `e => e.Email.ToLower()` is a pitfall; use `.IgnoreCasing()`.
- `builder.Unique<TEvent>(name:, message:)` enforces "one event of this type per event source" via the fluent builder (distinct from class-level `[Unique]`).
- Treat a constraint violation as a validation result on the command append — never as a thrown exception.

**Spec the constraint** with `EventScenario`: seed the conflicting state, append again, and assert `ShouldHaveConstraintViolationFor(<Module>ConstraintNames.UniqueX)` — **the constraint name, never the message**. Use per-test unique values (`$"{Guid.NewGuid():N}"`) so specs aren't order-dependent. For a release, append the removal event then assert the value can be re-claimed.

## After adding

1. Add a spec for the failure case — assert **both** `ShouldNotBeSuccessful()` and `ShouldHaveValidationErrors()` (or `ShouldHaveConstraintViolationFor(name)` for constraints). See the `write-specs` / `write-specs-events` skills.
2. Build clean (Debug and Release) and run the specs.
3. Fix all failures before completing.
