```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DecFunctionsAccountOpened(string Number);

[EventType]
public record DecFunctionsMoneyDeposited(decimal Amount);

[EventType]
public record DecFunctionsMoneyWithdrawn(decimal Amount);

public record DecFunctionsAccount(string Number, decimal Balance);

public class DecFunctionsAccountProjection : IProjectionFor<DecFunctionsAccount>
{
    public void Define(IProjectionBuilderFor<DecFunctionsAccount> builder) => builder
        .AutoMap()
        .From<DecFunctionsAccountOpened>(_ => _
            .Set(m => m.Balance).ToValue(0m))
        .From<DecFunctionsMoneyDeposited>(_ => _
            .Add(m => m.Balance).With(e => e.Amount))
        .From<DecFunctionsMoneyWithdrawn>(_ => _
            .Subtract(m => m.Balance).With(e => e.Amount));
}
```
