```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Specifications;

namespace ExplicitScenarioExample
{
    public record Credited(int Amount);
    public record CreditNoted(int Amount);
    public record Balance(int Amount);

    public class BalanceReducer
    {
        public Balance Reduce(Credited @event, Balance? current) =>
            new((current?.Amount ?? 0) + @event.Amount);
    }

    public class Notifier
    {
        public CreditNoted On(Credited @event) => new(@event.Amount);
    }

    public static class Scenarios
    {
        public static async Task Verify()
        {
            var options = new ChronicleOptions();
            options.ExplicitArtifacts.RegisterEventType<Credited>().RegisterEventType<CreditNoted>()
                .RegisterReducer<BalanceReducer>().RegisterReactor<Notifier>();
            var defaults = new Defaults(new InMemoryClientArtifactsProvider(), null, options);
            var source = EventSourceId.New();

            var readModel = new ReadModelScenario<Balance>(null, defaults);
            await readModel.Given.ForEventSource(source).Events(new Credited(3), new Credited(4));
            readModel.Instance!.Amount.ShouldEqual(7);

            using var reactor = new ReactorScenario<Notifier>(defaults);
            await reactor.Given.ForEventSource(source).Events(new Credited(3));
            reactor.ShouldHaveProduced<CreditNoted>(_ => _.Amount == 3);

            using var events = new EventScenario(defaults);
            (await events.EventLog.Append(source, new Credited(5))).ShouldBeSuccessful();
        }
    }
}
```
