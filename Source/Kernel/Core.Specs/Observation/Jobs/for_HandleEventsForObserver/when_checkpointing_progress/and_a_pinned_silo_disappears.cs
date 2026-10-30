// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_checkpointing_progress;

public class and_a_pinned_silo_disappears : given.a_debouncing_job_step
{
    Exception _error;

    async Task Establish()
    {
        _observerServiceClient.FlushAllFor(Arg.Any<ObserverDetails>(), Arg.Any<bool>()).Returns(
            Task.FromResult<IReadOnlyDictionary<SiloAddress, Guid[]>>(new Dictionary<SiloAddress, Guid[]> { [SiloAddress.Zero] = [Guid.NewGuid()] }),
            Task.FromResult<IReadOnlyDictionary<SiloAddress, Guid[]>>(new Dictionary<SiloAddress, Guid[]>()));
        await _jobStep.FlushCheckpoint(start: true);
        await _jobStep.ReportNewSuccessfullyHandledEvent(1UL);
        await _jobStep.ReportNewSuccessfullyHandledEvent(2UL);
    }

    async Task Because() => _error = await Catch.Exception(() => _jobStep.ReportNewSuccessfullyHandledEvent(3UL));

    [Fact] void should_fail_the_report() => _error.ShouldBeOfExactType<BulkWindowLost>();
    [Fact] void should_leave_the_durable_checkpoint_unchanged() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_persist_the_unconfirmed_position() => _stateStorageStats.Writes.ShouldEqual(0);
}
