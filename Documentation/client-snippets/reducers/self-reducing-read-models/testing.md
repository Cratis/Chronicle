```csharp
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Specifications;
using Xunit;

// Uses SelfAccountBalance, SelfBalanceAdded, and SelfBalanceCleared declared above.
public class when_recreating_a_self_reduced_balance : Specification
{
    readonly ReadModelScenario<SelfAccountBalance> _scenario = new();

    SelfAccountBalance? _beforeRemoval;
    SelfAccountBalance? _afterRemoval;

    async Task Because()
    {
        await _scenario.Given.ForEventSource("account-42").Events(new SelfBalanceAdded(10m), new SelfBalanceAdded(5m));
        _beforeRemoval = _scenario.Instance;
        await _scenario.Given.ForEventSource("account-42").Events(new SelfBalanceCleared());
        _afterRemoval = _scenario.Instance;
        await _scenario.Given.ForEventSource("account-42").Events(new SelfBalanceAdded(2m));
    }

    [Fact] void should_accumulate_the_additions() => _beforeRemoval!.Balance.ShouldEqual(15m);
    [Fact] void should_remove_the_instance() => _afterRemoval!.ShouldBeNull();
    [Fact] void should_start_a_new_balance_after_removal() =>
        _scenario.Instance!.Balance.ShouldEqual(2m);
}
```
