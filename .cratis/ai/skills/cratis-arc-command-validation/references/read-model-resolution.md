<!-- cratis-ai-managed: skills/cratis-arc-command-validation/references/read-model-resolution.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Read-model resolution for validation

Resolution does not key off `[ReadModel]`: a model becomes injectable because a
Chronicle projection or reducer, an EF Core `DbSet<T>`, or a MongoDB collection
backs it. Registration is global by type; keeping a model in the slice is a
convention, not a resolver restriction.

The default lookup uses the command's resolved event-source id. It does not
infer a key from the read-model type or an arbitrary similarly named property.
Use `[LookUpBy(nameof(Command.Property))]` for a command-property key; for a
computed key use `IDecisionReads.Get` in `Provide()` under read protection `On`.
An ordinary `IReadModels.GetInstanceById` there is refused under `On`; outside
protected work it is an advisory read, not a consistency guard.

A missing instance resolves to `null`, not a default object. A nullable parameter
receives that null; a non-nullable one produces a validation failure (400).
`ARC0006` warns on a non-nullable command-scoped read-model parameter.

Validators own missing-model refusals, not `Provide()`. A non-nullable read-model
constructor parameter on `CommandValidator<TCommand>` means the model must exist;
missing data is refused as `ReadModelDoesNotExistForCommand`. For a custom message,
take a nullable model and add a validator rule. This illustrative excerpt assumes
`PlaceOrder` has an `OrderId` root and a separate `CustomerId`, and `Customer` is a
read model. Use `Cratis.Arc.Commands` and `FluentValidation`:

```csharp
/// <summary>Requires an existing customer for an order.</summary>
public class PlaceOrderValidator : CommandValidator<PlaceOrder>
{
    /// <summary>Builds the customer-existence rule.</summary>
    /// <param name="customer">The customer resolved by the command's customer id.</param>
    public PlaceOrderValidator(
        [LookUpBy(nameof(PlaceOrder.CustomerId))] Customer? customer)
    {
        RuleFor(_ => customer).NotNull().WithMessage("Customer must exist.");
    }
}
```

Use `[LookUpBy(nameof(PlaceOrder.CustomerId))]` because this model is keyed by a
command property other than the root event-source id. `Provide()` is for acquiring
inputs the valid command needs, and only short-circuits when acquisition yields
nothing usable; it does not own this existence rule. Use
`Result<TEvent, ValidationResult>` in `Handle()` only when a rejection depends on
the event decision, not as a second copy of the validator rule. Do not throw for
normal business rejection or return a nullable event.

Read **cratis-arc-command** for the full resolution and nullability reference,
including passive projections where absence is not null.
