// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_checkpointing_progress;

public class and_the_flush_timer_fires : given.a_debouncing_job_step
{
    async Task Establish() => await _jobStep.ReportNewSuccessfullyHandledEvent(1UL);

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_flush_buffered_writes_before_checkpointing() => _observerServiceClient.Received(1).FlushAllFor(Arg.Any<ObserverDetails>(), true);
    [Fact] void should_persist_the_flushed_position() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)1UL);
    [Fact] void should_persist_once() => _stateStorageStats.Writes.ShouldEqual(1);
}
