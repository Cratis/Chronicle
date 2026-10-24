// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_checkpointing_progress;

public class and_an_older_position_is_reported : given.a_debouncing_job_step
{
    async Task Because()
    {
        await _jobStep.ReportNewSuccessfullyHandledEvent(4UL);
        await _jobStep.FlushCheckpoint();
        await _jobStep.ReportNewSuccessfullyHandledEvent(2UL);
        await _jobStep.FlushCheckpoint();
    }

    [Fact] void should_never_persist_the_older_position() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)4UL);
    [Fact] void should_not_write_the_same_frontier_twice() => _stateStorageStats.Writes.ShouldEqual(1);
}
