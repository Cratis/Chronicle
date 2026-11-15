```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Specifications;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

[EventType]
public record ParcelShipped(string TrackingNumber);

public class CarrierRejected(string trackingNumber) : Exception($"The carrier rejected parcel '{trackingNumber}'");

public interface ICarrier
{
    Task<IEnumerable<string>> Register(IEnumerable<string> trackingNumbers);
}

public class ParcelTracking(ICarrier carrier) : IReactor
{
    [Observes<ParcelShipped>]
    public async Task Register(EventBatch batch, BatchContext context)
    {
        var rejected = await carrier.Register(batch.Select(e => ((ParcelShipped)e.Event).TrackingNumber));
        foreach (var e in batch.Where(e => rejected.Contains(((ParcelShipped)e.Event).TrackingNumber)))
        {
            batch.Fail(e.Context, new CarrierRejected(((ParcelShipped)e.Event).TrackingNumber));
        }
    }
}

public class when_the_carrier_rejects_one_parcel : Specification
{
    readonly EventSourceId _accepted = EventSourceId.New();
    readonly EventSourceId _rejected = EventSourceId.New();
    readonly ICarrier _carrier = Substitute.For<ICarrier>();
    ReactorScenario<ParcelTracking> _scenario = null!;

    void Establish()
    {
        _carrier.Register(Arg.Any<IEnumerable<string>>()).Returns(["TRACK-2"]);
        _scenario = new(services => services.AddSingleton(_carrier));
    }

    Task Because() => _scenario.Given.Page(
        new(_accepted, new ParcelShipped("TRACK-1")),
        new(_rejected, new ParcelShipped("TRACK-2")));

    [Fact] void should_register_both_parcels_in_one_call() => _carrier.Received(1).Register(Arg.Any<IEnumerable<string>>());
    [Fact] void should_fail_the_rejected_parcel() => _scenario.ShouldHaveFailed(_rejected);
    [Fact] void should_fail_nothing_else() => _scenario.PartitionOutcomes.Count.ShouldEqual(1);
}
```
