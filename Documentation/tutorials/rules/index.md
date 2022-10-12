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

Following the [Bank sample](../../../Samples/Banking/Bank/) we can imagine a part of the system that enables
the closing of an existing account. We want to make sure we don't allow closing accounts that has either a
positive or a negative balance.

Start by creating a file in your domain (./Domain/Accounts/Debit) called `AccountMustBeEmptyAttribute.cs`.
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

The model based rules are built on top of [FluentValidation](https://docs.fluentvalidation.net/en/latest/) giving you a fluent interface
to express rules. If your actions take complex objects as arguments, the fluent interface enables you have all the rules for the type
in one place. With the FluentValidation API you also have more flexibility with the rules, as you can express conditionals for the rules,
something the attribute model does not allow for unless one hand-rolls this type of flexibility into the rules themselves.

Following the [Bank sample](../../../Samples/Banking/Bank/) we have the possibility to open a bank account. The bank accounts can have a friendly
name associated with it. This name makes sense to make sure is unique.

The action that opens the account takes a complex type; a command:

```csharp
public record OpenDebitAccount(AccountId AccountId, AccountName Name, PersonId Owner);
```

With this command we can now create rules for it. Start by creating a file in your domain (./Domain/Accounts/Debit) called `OpenDebitAccountRules.cs`.
Scaffold it with the following:

```csharp
public class OpenDebitAccountRules : RulesFor<OpenDebitAccountRules, OpenDebitAccount>
{
    public override RuleId Identifier => "9c09c285-0eea-4632-ac2d-0d23c7ac10ba";
}
```

The first generic parameter is telling that the type itself will be the holder of the state. Note that this has to be the rules type itself.
For the second parameter we specify the command type; `OpenDebitAccount`. With this in place the system will automatically discover the
rule and call it when an action is called with it as payload.

Next thing we need is to give it some state that we can add rules for:

```csharp
public class OpenDebitAccountRules : RulesFor<OpenDebitAccountRules, OpenDebitAccount>
{
    public override RuleId Identifier => "9c09c285-0eea-4632-ac2d-0d23c7ac10ba";

    // The state we want to have rules for
    public IEnumerable<AccountName> Accounts { get; set; } = Array.Empty<AccountName>();

    // Define method that gets called to describe the projected state from events
    public override void DefineState(IProjectionBuilderFor<OpenDebitAccountRules> builder) => builder
        .Children(_ => _.Accounts, _ => _
            .IdentifiedBy(_ => _)
            .From<DebitAccountOpened>(_ => _.UsingKey(_ => _.Name)));
}
```

The rules are declared by adding a constructor where one describes rules for properties.
In addition to simple value rules like `.NotEmpty()` or `.Length()` there is a rule called `.Must()`
that takes a callback in the form of `Func<TProperty, bool>` that can be leveraged.

Lets add a specific rule called `BeUniqueName` in the form of a private method and a constructor
that leverages it:

```csharp
public class OpenDebitAccountRules : RulesFor<OpenDebitAccountRules, OpenDebitAccount>
{
    public override RuleId Identifier => "9c09c285-0eea-4632-ac2d-0d23c7ac10ba";

    public IEnumerable<AccountName> Accounts { get; set; } = Array.Empty<AccountName>();

    public OpenDebitAccountRules()
    {
        // Adding a rule to make sure the account name is unique
        RuleFor(_ => _.Name).Must(BeUniqueName).WithMessage("Account with name already exists");
    }

    public override void DefineState(IProjectionBuilderFor<OpenDebitAccountRules> builder) => builder
        .Children(_ => _.Accounts, _ => _
            .IdentifiedBy(_ => _)
            .From<DebitAccountOpened>(_ => _.UsingKey(_ => _.Name)));

    // The actual rule
    bool BeUniqueName(AccountName name) => !Accounts.Any(_ => _.Equals(name));
}
```

Having the rules within the class like this can be convenient, but you might find that you want to have
some rules be reusable or even more generic and applicable as abstract concepts.
Cratis provides a method in the base class called `RuleForState()` that lets you fluently describe rules per projected state property.

> Note: Out of the box Cratis provides some rules, but you can easily [extend it](../../recipes/rules/extending-rules.md).

Lets change to using a generic `.Unique()` rule:

```csharp
public class OpenDebitAccountRules : RulesFor<OpenDebitAccountRules, OpenDebitAccount>
{
    public override RuleId Identifier => "9c09c285-0eea-4632-ac2d-0d23c7ac10ba";

    public IEnumerable<AccountName> Accounts { get; set; } = Array.Empty<AccountName>();

    public OpenDebitAccountRules()
    {
        // Adding a rule to make sure the account name is unique
        RuleForState(_ => _.Accounts)
            .Unique(_ => _.Name)    // The Unique() rule lets you specify which property from the command it should compare against.
            .WithMessage("Account with name already exists");

        // Regular FluentValidation rule for command properties
        RuleFor(_ => _.Name).NotEmpty().WithMessage("You have to specify a name");
    }

    public override void DefineState(IProjectionBuilderFor<OpenDebitAccountRules> builder) => builder
        .Children(_ => _.Accounts, _ => _
            .IdentifiedBy(_ => _)
            .From<DebitAccountOpened>(_ => _.UsingKey(_ => _.Name)));
}
```

> Note: In the constructor as you notice, we also added a regular [FluentValidation rules](https://docs.fluentvalidation.net/en/latest/#example) that is related
> to the command. These rules can be input validation or anything.
