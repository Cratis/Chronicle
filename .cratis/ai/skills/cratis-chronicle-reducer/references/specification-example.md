<!-- cratis-ai-managed: skills/cratis-chronicle-reducer/references/specification-example.md -->
# Reducer specification example

This reference holds a worked direct-call and `ReadModelScenario` example for a self-reducing model.

```csharp
var next = AccountBalance.Reduce(new FundsCredited(new Money(10m)), null);
next.Total.Value.ShouldEqual(10m);

using var scenario = new ReadModelScenario<AccountBalance>();
await scenario.Given.ForEventSource("account-42").Events(
    new FundsCredited(new Money(10m)),
    new BalanceCleared(),
    new FundsCredited(new Money(3m)));
scenario.Instance!.Total.Value.ShouldEqual(3m);
```
