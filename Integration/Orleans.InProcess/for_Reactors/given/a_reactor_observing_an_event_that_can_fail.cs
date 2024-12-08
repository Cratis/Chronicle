// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Integration.Base;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.given;

public class a_reactor_observing_an_event_that_can_fail(GlobalFixture globalFixture) : IntegrationSpecificationContext(globalFixture)
{
    public TaskCompletionSource Tcs;
    public ReactorThatCanFail Observer;
    public IObserver ReactorObserver;
    public override IEnumerable<Type> Reactors => [typeof(ReactorThatCanFail)];
    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
    public Concepts.Observation.ObserverId ObserverId;

    protected override void ConfigureServices(IServiceCollection services)
    {
        Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Observer = new ReactorThatCanFail(Tcs);
        services.AddSingleton(Observer);
    }

    async Task Establish()
    {
        ReactorObserver = GetObserverFor<ReactorThatCanFail>();
        var reactorObserverState = await ReactorObserver.GetState();
        ObserverId = reactorObserverState.Id;
    }

    protected Task<IEnumerable<FailedPartition>> GetFailedPartitions() => EventStore.Connection.Services.Observers.GetFailedPartitionsForObserver(new()
    {
        EventStoreName = EventStore.Name.Value,
        Namespace = Concepts.EventStoreNamespaceName.Default,
        ObserverId = ObserverId
    });
}
