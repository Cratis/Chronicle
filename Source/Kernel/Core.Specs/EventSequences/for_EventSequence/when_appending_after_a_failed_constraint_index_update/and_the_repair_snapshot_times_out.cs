// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_after_a_failed_constraint_index_update;

public class and_the_repair_snapshot_times_out : given.an_event_sequence_with_repairable_indexes
{
    readonly given.RepairClock _clock = new();
    readonly TaskCompletionSource _write = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly given.RecordingEventSequenceLogger _repairLogger = new();
    bool _pendingBeforeSnapshotDeadline;
    protected override EventSequenceNumber InitialNext => 1;

    protected override Task<EventSequence> CreateEventSequence()
    {
        _eventSequenceLogger = _repairLogger;
        return base.CreateEventSequence();
    }

    async Task Establish()
    {
        await Seed(0);
        _eventSequence.ConstraintIndexTimeProvider = _clock;
        _snapshots.BeforeWrite = () =>
        {
            _clock.Advance(TimeSpan.FromMilliseconds(249));
            return _write.Task;
        };
        _eventSourceId = EventSourceId.New();
    }

    async Task Because()
    {
        var append = AppendAnEvent();
        _pendingBeforeSnapshotDeadline = !append.IsCompleted;
        _clock.Advance(TimeSpan.FromMilliseconds(1));
        _result = await append.WaitAsync(TimeSpan.FromSeconds(2), TimeProvider.System);
    }
    void Destroy() => _write.TrySetCanceled();

    [Fact] void should_give_the_snapshot_its_own_250_millisecond_bound() => _pendingBeforeSnapshotDeadline.ShouldBeTrue();
    [Fact] void should_not_mark_the_repaired_index_pending_again() => _result.Errors.ShouldBeEmpty();
    [Fact] void should_validate_against_the_repaired_claim() => _result.ConstraintViolations.ShouldNotBeEmpty();
    [Fact] void should_leave_only_persistence_lag() => _snapshots.Written.ShouldBeNull();
    [Fact] void should_warn_that_only_the_snapshot_failed() => _repairLogger.Warnings.ShouldContain(message => message.Contains("were repaired but the snapshot", StringComparison.Ordinal));
    [Fact] void should_not_report_a_failed_repair() => _repairLogger.Errors.ShouldBeEmpty();
}
