# Validation

The concept of validation is to make sure all your inputs are in a valid form before hitting your logic.
Validation is different from [rules](./rules.md) in the sense that it is focused on user input and sanitizing the values
coming as input. While [rules](./rules.md) are stateful and depends on state from your system to be able to validate
for correctness.

## Value based validators

The value based validation leverages what is already in the ASP.NET Core pipelines [custom attributes](https://docs.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-6.0#custom-attributes).

You can leverage this by using attributes such as the `[Required]` attribute:

```csharp
public record OpenDebitAccount(
    [Required] AccountId AccountId,
    AccountDetails Details);
```

Attributes are compile time and just represent metadata, the downside of this is that if you're having context dependent rules or need
to inject localized strings or similar you won't be able to get to it.

## Fluent Validation

Another alternative approach to validation is to use [FluentValidation](https://docs.fluentvalidation.net).
Cratis comes with this all setup and automatically hooks up different types of validators for specific purposes.

### Base Validator

The formalized validator types (`CommandValidator`, `QueryValidator` and `ConceptValidator`) all derive from a
type called `BaseValidator`. This base type provides methods for defining validation rules for known `ConceptAs<>`
primitives and will unwrap the inner `Value` property automatically, providing you a clean way of validating the
concepts inner primitive type without considering the `Value` property.

However, if you're hooking up validators for the actual concept, you need to use the method `RuleForConcept()`
to work directly with the concept. The `IRuilBuilderInitial` type returned would then be for the actual concept and
not its primitive type its encapsulating.

### Discoverable validators

Validators can be automatically discovered. This done through the discovery of anything that implements the marker
interface `IDiscoverableValidator<>`. The formalized types for Commands, Queries and Concepts all implement this.
There is also a base type that can be used, which inherits from `BaseValidator` to give you the additional functionality
it provides. All you need to do is inherit from `DiscoverableValidator<>`.

### Command Validator

To write command validators, all you need to do is implement the `CommandValidator<>` class and create
rules for your properties.

Lets say you have a command as follows:

```csharp
public record OpenDebitAccount(
    AccountId AccountId,
    AccountDetails Details);
```

A validator for this could then be as follows:

```csharp
using Aksio.Cratis.Applications.Commands;

public class OpenDebitAccountValidator : CommandValidator<OpenDebitAccount>
{
    public OpenDebitAccountValidator()
    {
        RuleFor(_ => _.Details.Name).NotEmpty().WithMessage("Name is required");
        RuleFor(_ => _.Details.Owner).NotNull().WithMessage("Owner is required");
        RuleFor(_ => _.Details.IncludeCard).NotNull().WithMessage("Include card should be specified");
    }
}
```

### Query Validator

**Watch this space; MORE TO COME**

> Note: Since queries are not represented using objects today, FluentValidation does not make sense.
> However, we have an [issue](https://github.com/aksio-insurtech/Cratis/issues/670) for making it possible to represent input to a query as an object.
> Until then you can use attribute based validation or make your query parameters a concept and use the concept validator approach.
> You could also implement validators using the `DiscoverableValidator<>`.

### Concept Validator

When one is using [domain concepts](../fundamentals/concepts.md), you have the opportunity to create a validator for
the concept that will automatically be used as part of the ASP.NET Core validation pipeline.

The benefit of this approach is that you can reuse validation rules and they will automatically implicitly be hooked
up, leading to not have to remember to explicitly add every rule for reused concepts.

The tradeoff of this is obviously that your rules are scattered around.

Say you have a concept as follows:

```csharp
public record AccountName(string Value) : ConceptAs<string>(Value);
```

By inheriting the `ConceptValidator<>` type you can create rules for the concept:

```csharp
using Aksio.Cratis.Applications.Validation;

public class AccountNameValidator : ConceptValidator<AccountName>
{
    public AccountNameValidator()
    {
        RuleFor(_ => _).Length(0, 16).WithMessage("Account name has to be less than 16 characters");
    }
}
```
