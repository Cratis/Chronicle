// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Jobs;
using Orleans.TestKit;

namespace Cratis.Chronicle.Grains.Observation.Jobs.for_CatchUpObserver;

public class when_step_completes : given.a_catchup_observer_and_a_request
{
    IObserver _observer;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _silo.AddProbe(_ => _observer);
        _state.HandledCount = 42;
    }

    async Task Because() => await _job.WrappedOnStepCompleted(Guid.NewGuid(), JobStepResult.Succeeded(new HandleEventsForPartitionResult(43)));

    [Fact] void should_add_the_handled_events_to_the_handled_count() => _state.HandledCount.ShouldEqual(42 + 43);
}
