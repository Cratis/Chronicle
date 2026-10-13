// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_checkpointing_progress;

public class and_the_flush_has_not_completed : given.a_debouncing_job_step
{
    readonly TaskCompletionSource<IReadOnlyDictionary<SiloAddress, Guid[]>> _flush = new(TaskCreationOptions.RunContinuationsAsynchronously);
    EventSequenceNumber _positionDuringFlush;
    int _writesDuringFlush;

    void Establish() => _observerServiceClient.FlushAllFor(Arg.Any<ObserverDetails>(), Arg.Any<bool>()).Returns(_flush.Task);

    async Task Because()
    {
        await _jobStep.ReportNewSuccessfullyHandledEvent(1UL);
        await _jobStep.ReportNewSuccessfullyHandledEvent(2UL);
        var reporting = _jobStep.ReportNewSuccessfullyHandledEvent(3UL);
        _positionDuringFlush = _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber;
        _writesDuringFlush = _stateStorageStats.Writes;
        _flush.SetResult(new Dictionary<SiloAddress, Guid[]> { [SiloAddress.Zero] = [] });
        await reporting;
    }

    [Fact] void should_not_expose_unflushed_progress_to_any_state_write() => _positionDuringFlush.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_persist_progress_before_the_flush_completes() => _writesDuringFlush.ShouldEqual(0);
    [Fact] void should_persist_progress_after_the_flush_completes() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
}
