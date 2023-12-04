// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Observation.States;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs.for_CatchUpObserver;

[Collection(OrleansClusterCollection.Name)]
public class when_completed : given.a_catchup_observer_and_a_request
{
    Mock<IObserver> observer;

    public when_completed(OrleansClusterFixture clusterFixture)
        : base(clusterFixture)
    {
    }

    void Establish()
    {
        observer = new();
        grain_factory.Setup(_ => _.GetGrain<IObserver>(state.Request.ObserverId, state.Request.ObserverKey, null)).Returns(observer.Object);
        state.HandledCount = 42;
    }

    async Task Because() => await job.OnCompleted();

    [Fact] void should_report_handled_events() => observer.Verify(_ => _.ReportHandledEvents(state.HandledCount), Once);
    [Fact] void should_transition_to_routing() => observer.Verify(_ => _.TransitionTo<Routing>(), Once);
}
