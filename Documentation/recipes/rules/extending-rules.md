# Extending rules

As part of your domain logic you typically have rules that govern whats allowed or not.
Some of these rules are reusable rules one wants to apply in multiple places.
A good idea would then to encapsulate this and make it accessible to all the places.

In the base class `RulesFor<,>` used by stateful consistent rules the method `RuleForState()`
starts defining rules for state on the rules type you create.

To create your own custom reusable rule there are two components:

* A type that implements FluentValidations `PropertyValidator<>`.
* Extension method that extends the `IStateRuleBuilder<>` returned from `RuleForState()` method.

Lets take the `Unique` rule that Cratis offers. Its job is to make sure a value coming in
is not already in a collection.

Lets start with the `PropertyValidator` part:

```csharp
public class UniqueInstanceValidator<T, TProperty> : PropertyValidator<T, TProperty>
    where TProperty : IEnumerable   // Rule only words on enumerables
{
    readonly Func<object, object> _getValue;

    public override string Name => nameof(UniqueInstanceValidator<T, TProperty>);

    // Takes a callback for getting the value to check if already exists
    public UniqueInstanceValidator(Func<object, object> getValue)
    {
        _getValue = getValue;
    }

    public override bool IsValid(ValidationContext<T> context, TProperty value)
    {
        var parent = (context as IValidationContext)!.ParentContext;

        // Use the callback passed in to get the value from the instance to validate
        var valueToCheck = _getValue(parent.InstanceToValidate);
        foreach (var element in value)
        {
            if (element.Equals(valueToCheck))
            {
                return false;
            }
        }

        return true;
    }
}
```

With this in place, we can now add an extension method that will make this feel nice to work with:

```csharp
public static IRuleBuilderOptions<TState, TProperty> Unique<TState, TCommand, TProperty, TCommandProperty>(this IStateRuleBuilder<TState, TCommand, TProperty> ruleBuilder, Expression<Func<TCommand, TCommandProperty>> expression)
    where TState : RulesFor<TState, TCommand>
    where TProperty : IEnumerable<TCommandProperty>
{
    // Cratis has extension methods for getting property information from Expression
    var propertyInfo = expression.GetPropertyInfo();
    var getValue = (object instance) => propertyInfo.GetMethod!.Invoke(instance, Array.Empty<object>());
    return ruleBuilder.SetValidator(new UniqueInstanceValidator<TState, TProperty>(getValue));
}
```

The input to the validator can be anything or nothing, you decide this entirely.

With this in place, we can use it in a rules implementation:

```csharp
public class OpenDebitAccountRules : RulesFor<OpenDebitAccountRules, OpenDebitAccount>
{
    public override RuleId Identifier => "9c09c285-0eea-4632-ac2d-0d23c7ac10ba";

    public IEnumerable<AccountName> Accounts { get; set; } = Array.Empty<AccountName>();

    public OpenDebitAccountRules()
    {
        RuleFor(_ => _.Name).Must(). NotEmpty().WithMessage("You have to specify a name");
        RuleForState(_ => _.Accounts)
            // Usage of the rule
            .Unique(_ => _.Name)
            .WithMessage("Account with name already exists");
    }

    public override void DefineState(IProjectionBuilderFor<OpenDebitAccountRules> builder) => builder
        .Children(_ => _.Accounts, _ => _
            .IdentifiedBy(_ => _)
            .From<DebitAccountOpened>(_ => _.UsingKey(_ => _.Name)));


    bool BeUniqueName(AccountName name) => !Accounts.Any(_ => _.Equals(name));
}
```
