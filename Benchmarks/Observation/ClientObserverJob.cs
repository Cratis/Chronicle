// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Dynamic;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Kernel.MongoDB;
using Aksio.Cratis.Observation;
using Benchmark.Model;

namespace Benchmarks.Observation;

public class ClientObserverJob : BenchmarkJob
{
    static readonly PersonId _personId = new(Guid.Parse("c7073ebc-87eb-48c8-b747-908fa0acdd17"));
    static readonly MaterialId _materialId = new(Guid.Parse("e2701406-2b28-4e9b-bc78-36a356682f13"));

    IEnumerable<AppendedEvent> _eventsToHandle = Enumerable.Empty<AppendedEvent>();

    [Params(10, 100, 1000)]
    public int EventsToCommit { get; set; } = 1000;

    protected override IEnumerable<Type> EventTypes => new[]
    {
        typeof(ItemAddedToCart),
        typeof(ItemRemovedFromCart),
        typeof(QuantityAdjustedForItemInCart),
    };

    protected IObserverSupervisor ObserverSupervisor { get; private set; } = null!;

    [IterationSetup]
    public void CleanEventStore()
    {
        SetExecutionContext();

        Database?.DropCollection(CollectionNames.Observers);
    }

    [Benchmark]
    public async Task DoStuff()
    {
        foreach (var @event in _eventsToHandle)
        {
            await ObserverSupervisor.Handle(@event, true);
        }
    }

    protected override void Setup()
    {
        SetExecutionContext();
        base.Setup();

        var key = new ObserverKey(GlobalVariables.MicroserviceId, GlobalVariables.TenantId, EventSequenceId.Log);
        ObserverSupervisor = GrainFactory.GetGrain<IObserverSupervisor>(Guid.Parse(CartObserver.Identifier), key);

        var eventTypes = EventTypes.ToArray();

        _eventsToHandle = Enumerable.Range(0, EventsToCommit).Select(index =>
        {
            var @event = GetEventInstanceFor(index);
            var appendedEvent = AppendedEvent.EmptyWithEventType(@event.GetType().GetEventType());
            return appendedEvent with
            {
                Content = @event.AsExpandoObject(),
                Metadata = appendedEvent.Metadata with
                {
                    SequenceNumber = (ulong)index
                }
            };
        }).ToArray();
    }

    object GetEventInstanceFor(int index)
    {
        switch (index % (EventTypes.Count() - 1))
        {
            case 0:
                return new ItemAddedToCart(_personId, _materialId, 1);
            case 1:
                return new ItemRemovedFromCart(_personId, _materialId);
        }

        return new QuantityAdjustedForItemInCart(_personId, _materialId, 1);
    }
}
