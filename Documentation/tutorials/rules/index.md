# Rules

When performing operations (referred to as where we do **writes** or perform **commands**),
one needs to be able to validate the correctness of the input and also that one does not violate
any domain specific rules associated with the action being performed.

The domain specific rules tend to rely on state of what has happened in the system and needs
to do so with a strong consistent guarantee. In an event sourced system the only way this can
be guaranteed is by looking at the events that was produced and typically project to a current
state that we can perform assertions against.

Cratis supports 2 approaches to defining rules:

* Value based rules based on an attribute model.
* Model based rules based on a fluent interface model.

## Value based rules

The value based rules are building on top of the ASP.NET Core pipelines [custom attributes](https://docs.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-6.0#custom-attributes).

Following the [Bank sample](../../../Samples/Bank/) we can imagine a part of the system that enables
the closing of an existing account. We want to make sure we don't allow closing accounts that has either a
positive or a negative balance.

Start by creating a file in your domain (./Domain/Accounts/Debit) called `AccountMustBeEmptyAttribute`.
Scaffold it with the following:

```csharp
namespace Domain.Accounts;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
public sealed class AccountMustBeEmptyAttribute : RuleAttribute
{
    public override RuleId Identifier => "bc2042a9-3908-4ac4-8637-b2058ba8cead";

    protected override bool IsValid(object? value)
    {
        return true;
    }
}
```

> Note: Every rule has a unique identifier, two rules can't share same identifier.

 On the rule itself we can now define state and a projection that hydrates this state from the event store.

```csharp
namespace Domain.Accounts;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
public sealed class AccountMustBeEmptyAttribute : RuleAttribute
{
    public override RuleId Identifier => "bc2042a9-3908-4ac4-8637-b2058ba8cead";

    // The state we want to check
    public double Balance { get; set; }

    // Define method, discovered by Cratis
    public void DefineState(IProjectionBuilderFor<AccountMustBeEmptyAttribute> builder) => builder
        .From<DepositToDebitAccountPerformed>(_ => _
            .Add(m => m.Balance).With(e => e.Amount))
        .From<WithdrawalFromDebitAccountPerformed>(_ => _
            .Subtract(m => m.Balance).With(e => e.Amount));

    // Specific error message
    public override string FormatErrorMessage(string name) => $"Account must have 0 in balance. It has a balance of {Balance}.";

    protected override bool IsValid(object? value)
    {
        // Check if balance is 0
        return Balance == 0;
    }
}
```

> Note: The `DefineState()` is discovered by convention. It looks for a specific signature that is either `public` or `private`,
> returns `void` and takes one argument of `IProjectionBuilderFor<>` with the rule type as generic argument.
> Also note that the state property needs to be a `public` property with a setter.

We can then use this attribute on a controller action:

```csharp
[Route("/api/accounts/debit/{accountId}")]
public class DebitAccount : Controller
{
    [HttpPost("close")]
    public Task CloseDebitAccount(
        [FromRoute]
        [AccountMustBeEmpty(IsModelKey = true)]
        AccountId accountId) => _eventLog.Append(accountId, new DebitAccountClosed());
}
```

Notice that in usage, it is explicitly setting `IsModelKey = true`. This indicates that the value the rule is for can be used
as the key when getting a specific projected instance. In our case, we want it to project only the events for the specific account id.

## Model based rules

The model based rules are built on top of [FluentValidation](https://docs.fluentvalidation.net/en/latest/) giving you a fluent interface.
If your actions take complex objects as arguments, it could be a good fit to model the rules in a separate file.
Another aspect of the FluentValidation API is that it provides fluent APIs for dynamic rules that are applied based on conditions.
Something the attribute model does not allow for unless one hand-rolls this type of flexibility into the rules themselves.
