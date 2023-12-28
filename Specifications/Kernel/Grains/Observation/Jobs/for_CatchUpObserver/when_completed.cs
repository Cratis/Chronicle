// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Observation.States;
using Orleans.TestKit;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs.for_CatchUpObserver;

public class when_completed : given.a_catchup_observer_and_a_request
{
    Mock<IObserver> observer;

    void Establish()
    {
        observer = silo.AddProbe<IObserver>(((CatchUpObserverRequest)state_storage.State.Request).ObserverId, ((CatchUpObserverRequest)state_storage.State.Request).ObserverKey);
        state_storage.State.HandledCount = 42;
    }

    async Task Because() => await job.OnCompleted();

    [Fact] void should_report_handled_events() => observer.Verify(_ => _.ReportHandledEvents(state_storage.State.HandledCount), Once);
    [Fact] void should_transition_to_routing() => observer.Verify(_ => _.TransitionTo<Routing>(), Once);
}
