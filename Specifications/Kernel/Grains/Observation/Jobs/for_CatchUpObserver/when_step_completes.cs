// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Jobs;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs.for_CatchUpObserver;

public class when_step_completes : given.a_catchup_observer_and_a_request
{
    Mock<IObserver> observer;

    void Establish()
    {
        observer = new();
        grain_factory.Setup(_ => _.GetGrain<IObserver>(state.Request.ObserverId, state.Request.ObserverKey, null)).Returns(observer.Object);
        state.HandledCount = 42;
    }

    async Task Because() => await job.WrappedOnStepCompleted(Guid.NewGuid(), JobStepResult.Succeeded(new HandleEventsForPartitionResult(43)));

    [Fact] void should_add_the_handled_events_to_the_handled_count() => state.HandledCount.ShouldEqual(42 + 43);
}
