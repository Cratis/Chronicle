// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_checkpointing_progress;

public class and_the_flush_timer_fails : given.a_debouncing_job_step
{
    Exception _error;

    async Task Establish()
    {
        _observerServiceClient.FlushAllFor(Arg.Any<ObserverDetails>(), Arg.Any<bool>())
            .Returns(Task.FromException<IReadOnlyDictionary<SiloAddress, Guid[]>>(new ObserverFlushFailed()));
        await _jobStep.ReportNewSuccessfullyHandledEvent(1UL);
        await _silo.TimerRegistry.FireAllAsync();
    }

    async Task Because() => _error = await Catch.Exception(() => _jobStep.ReportNewSuccessfullyHandledEvent(2UL));

    [Fact] void should_fail_the_next_report() => _error.ShouldBeOfExactType<ObserverFlushFailed>();
    [Fact] void should_keep_the_durable_position() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_persist_unconfirmed_progress() => _stateStorageStats.Writes.ShouldEqual(0);
}
