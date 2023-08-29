// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Observation;
using Aksio.Execution;
using Benchmark.Model;

namespace Benchmarks.Observation;

public class ClientObserverJob : BenchmarkJob
{
    protected IObserverSupervisor ObserverSupervisor { get; private set; } = null!;

    protected override IEnumerable<Type> EventTypes => new[]
    {
        typeof(ItemAddedToCart)
    };

    [Benchmark]
    public async Task DoStuff()
    {
        var @event = AppendedEvent.EmptyWithEventType(typeof(ItemAddedToCart).GetEventType());
        await ObserverSupervisor.Handle(@event);
    }

    protected override void Setup()
    {
        SetExecutionContext();

        var key = new ObserverKey(GlobalVariables.MicroserviceId, TenantId.Development, EventSequenceId.Log);
        ObserverSupervisor = GrainFactory.GetGrain<IObserverSupervisor>(Guid.Parse(CartObserver.Identifier), key);

        base.Setup();
    }
}
