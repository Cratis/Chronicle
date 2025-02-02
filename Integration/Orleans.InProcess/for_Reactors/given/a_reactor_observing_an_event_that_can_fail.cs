// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Integration.Base;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.given;

public class a_reactor_observing_an_event_that_can_fail(GlobalFixture globalFixture, int numberOfObservations) : IntegrationSpecificationContext(globalFixture)
{
    public TaskCompletionSource[] Tcs;
    public ReactorThatCanFail[] Observers;
    public IObserver ReactorObserver;
    public override IEnumerable<Type> Reactors => [typeof(ReactorThatCanFail)];
    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
    public Concepts.Observation.ObserverId ObserverId;
    int _activationCount;

    protected override void ConfigureServices(IServiceCollection services)
    {
        Tcs = Enumerable.Range(0, numberOfObservations).Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
        Observers = Enumerable.Range(0, numberOfObservations).Select(index => new ReactorThatCanFail(Tcs[index])).ToArray();
        services.AddTransient(_ => Observers[_activationCount++]);
    }

    async Task Establish()
    {
        ReactorObserver = GetObserverForReactor<ReactorThatCanFail>();
        var reactorObserverState = await ReactorObserver.GetState();
        ObserverId = reactorObserverState.Id;
    }

    protected Task<IEnumerable<FailedPartition>> GetFailedPartitions() => ServicesAccessor.Services.FailedPartitions.GetFailedPartitions(new()
    {
        EventStore = EventStore.Name.Value,
        Namespace = Concepts.EventStoreNamespaceName.Default,
        ObserverId = ObserverId
    });
}
