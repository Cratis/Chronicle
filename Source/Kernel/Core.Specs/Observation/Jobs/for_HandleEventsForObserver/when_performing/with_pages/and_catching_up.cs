// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_pages;

public class and_catching_up : given.a_page_job_step
{
    void Establish() => _performState.EventObservationState = EventObservationState.None;

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_not_use_pages_for_catchup() => _pages.ShouldBeEmpty();
    [Fact] void should_keep_consecutive_partition_runs() => _handledBatches.Count.ShouldEqual(3);
}
