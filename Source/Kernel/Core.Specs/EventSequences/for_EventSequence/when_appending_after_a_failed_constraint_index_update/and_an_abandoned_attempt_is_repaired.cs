// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_after_a_failed_constraint_index_update;

public class and_an_abandoned_attempt_is_repaired : given.an_event_sequence_with_repairable_indexes
{
    readonly given.RepairClock _clock = new();
    readonly TaskCompletionSource _write = new(TaskCreationOptions.RunContinuationsAsynchronously);
    AppendResult _beforeConfirmation;
    bool _hang = true;

    async Task Establish()
    {
        _eventSequence.ConstraintIndexTimeProvider = _clock;
        _unique.Save(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>(), Arg.Any<Subject?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                if (!_hang) return _claims.Save(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<EventSequenceNumber>(2), call.ArgAt<UniqueConstraintValue>(3), call.ArgAt<string>(4), call.ArgAt<Subject?>(5), call.ArgAt<CancellationToken>(6));
                _clock.Advance(TimeSpan.FromSeconds(2));
                return _write.Task;
            });
        await AppendAnEvent();
        _hang = false;
        await given.ConstraintRepairTimer.Tick(_silo);
        _eventSourceId = EventSourceId.New();
        _beforeConfirmation = await AppendAnEvent();
        _write.TrySetResult();
    }

    async Task Because()
    {
        await given.ConstraintRepairTimer.Tick(_silo);
        _result = await AppendAnEvent();
    }

    [Fact] void should_refuse_until_a_confirming_timer_fold() => _beforeConfirmation.Errors.ShouldContain(AppendError.ConstraintIndexUnavailable);
    [Fact] void should_clear_the_abandoned_constraint_after_confirmation() => _result.Errors.ShouldBeEmpty();
    [Fact] void should_validate_against_the_repaired_claim() => _result.ConstraintViolations.ShouldNotBeEmpty();
}
