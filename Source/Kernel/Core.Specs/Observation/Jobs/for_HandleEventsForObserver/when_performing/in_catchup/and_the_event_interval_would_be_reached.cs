// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.in_catchup;

public class and_the_event_interval_would_be_reached : given.a_job_step_with_a_replay_event_interval
{
    void Establish()
    {
        _performState.EventObservationState = EventObservationState.Initial;
        SetupPages(AlternatingEvents(1, 5));
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_not_checkpoint_per_event_interval() => _flushes.Count.ShouldEqual(2);
}
