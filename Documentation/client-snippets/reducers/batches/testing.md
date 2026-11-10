```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Specifications;
using Xunit;

[EventType]
public record SeatsAdded(int Seats);

[EventType]
public record SeatsRemoved(int Seats);

public record Venue(int Seats)
{
    [Observes<SeatsAdded>]
    [Observes<SeatsRemoved>]
    public static Venue? Reduce(EventBatch events, Venue? current, BatchContext context) =>
        events.Aggregate(current ?? new Venue(0), (venue, e) => e.Event switch
        {
            SeatsAdded added => venue with { Seats = venue.Seats + added.Seats },
            SeatsRemoved removed => venue with { Seats = venue.Seats - removed.Seats },
            _ => venue
        });
}

public class when_seats_are_added_and_removed : Specification
{
    readonly ReadModelScenario<Venue> _scenario = new();

    Task Because() =>
        _scenario.Given
            .ForEventSource(EventSourceId.New())
            .Events(new SeatsAdded(100), new SeatsRemoved(20));

    [Fact] void should_fold_the_events_in_one_call() => _scenario.Instance!.Seats.ShouldEqual(80);
}
```
