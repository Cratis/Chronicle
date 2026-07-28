// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_checkpointing_progress;

public class and_the_checkpoint_interval_is_not_reached : given.a_debouncing_job_step
{
    async Task Because()
    {
        await _jobStep.ReportNewSuccessfullyHandledEvent(1UL);
        await _jobStep.ReportNewSuccessfullyHandledEvent(2UL);
    }

    [Fact] void should_not_persist_the_checkpoint_before_the_interval_is_reached() => _stateStorageStats.Writes.ShouldEqual(0);
    [Fact] void should_still_advance_the_checkpoint_in_memory() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)2UL);
}
