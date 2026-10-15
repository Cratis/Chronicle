// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_repairing_constraint_indexes_on_the_timer;

public class and_a_live_abandoned_write_is_applied_on_retry : given.an_event_sequence_with_repairable_indexes
{
    readonly given.RepairClock _clock = new();
    readonly TaskCompletionSource _retryScheduled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _lateWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
    AppendResult _beforeConfirmation;
    bool _pendingAfterRetry;
    int _attempts;
    EventSourceId _originalSource;
    UniqueConstraintValue _value;

    void Establish()
    {
        _originalSource = _eventSourceId;
        _eventSequence.ConstraintIndexTimeProvider = _clock;
        _clock.TimerCreated = due =>
        {
            if (due == TimeSpan.FromMilliseconds(100)) _retryScheduled.TrySetResult();
        };
        _unique.Save(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                if (++_attempts != 1) return _claims.Save(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<EventSequenceNumber>(2), call.ArgAt<UniqueConstraintValue>(3), call.ArgAt<string>(4), call.ArgAt<CancellationToken>(5));
                _value = call.ArgAt<UniqueConstraintValue>(3);
                _clock.Advance(TimeSpan.FromSeconds(1));
                return _lateWrite.Task;
            });
    }

    async Task Because()
    {
        var append = AppendAnEvent();
        await _retryScheduled.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _clock.Advance(TimeSpan.FromMilliseconds(100));
        await append.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _pendingAfterRetry = _stateStorage.State.ConstraintIndexWatermark == EventSequenceNumber.First;
        var claimType = _eventType;
        _eventType = new("Removed", EventTypeGeneration.First);
        await AppendAnEvent();
        _eventType = claimType;
        await given.ConstraintRepairTimer.Tick(_silo);
        _eventSourceId = EventSourceId.New();
        _beforeConfirmation = await AppendAnEvent();

        // The abandoned first attempt lands only after the first repair removed its claim.
        await _claims.Save(_originalSource, _uniqueDefinition, 0, _value);
        _lateWrite.TrySetResult();
        await given.ConstraintRepairTimer.Tick(_silo);
        _result = await AppendAnEvent();
    }

    void Destroy() => _lateWrite.TrySetCanceled();

    [Fact] void should_leave_the_successful_retry_pending_after_abandonment() => _pendingAfterRetry.ShouldBeTrue();
    [Fact] void should_require_a_confirmation_timer_turn() => _beforeConfirmation.Errors.ShouldContain(AppendError.ConstraintIndexUnavailable);
    [Fact] async Task should_remove_the_late_first_write() => (await _claims.GetFor(_originalSource, _uniqueDefinition)).ShouldBeEmpty();
    [Fact] void should_admit_only_after_confirmation() => _result.IsSuccess.ShouldBeTrue();
}
