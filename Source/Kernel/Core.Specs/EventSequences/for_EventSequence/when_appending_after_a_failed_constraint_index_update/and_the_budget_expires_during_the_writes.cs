// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_after_a_failed_constraint_index_update;

public class and_the_budget_expires_during_the_writes : given.an_event_sequence_with_repairable_indexes
{
    readonly given.RepairClock _clock = new();
    readonly EventSourceId _second = "second";
    AppendResult _unfinished;
    bool _firstWritten;
    bool _secondNotWritten;
    int _firstWrites;
    int _secondWrites;
    protected override EventSequenceNumber InitialNext => 2;

    async Task Establish()
    {
        await Seed(0);
        await Seed(1, _second, email: "other@example.com");
        _eventSequence.ConstraintIndexTimeProvider = _clock;
        _unique.Save(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>(), Arg.Any<Subject?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                if (call.ArgAt<EventSourceId>(0) == _second && ++_secondWrites == 1)
                {
                    _clock.Advance(TimeSpan.FromMilliseconds(4750));
                    return Task.FromCanceled(call.ArgAt<CancellationToken>(6));
                }
                if (call.ArgAt<EventSourceId>(0) != _second) _firstWrites++;
                return _claims.Save(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<EventSequenceNumber>(2), call.ArgAt<UniqueConstraintValue>(3), call.ArgAt<string>(4), call.ArgAt<Subject?>(5), call.ArgAt<CancellationToken>(6));
            });
    }

    async Task Because()
    {
        _unfinished = await AppendAnEvent();
        _firstWritten = (await _claims.GetFor(_eventSourceId, _uniqueDefinition)).Any();
        _secondNotWritten = !(await _claims.GetFor(_second, _uniqueDefinition)).Any();
        _clock.Advance(TimeSpan.FromSeconds(5));
        _eventSourceId = EventSourceId.New();
        _result = await AppendAnEvent();
    }

    [Fact] void should_keep_the_successfully_written_key() => _firstWritten.ShouldBeTrue();
    [Fact] void should_leave_the_remaining_key_unwritten_at_the_budget() => _secondNotWritten.ShouldBeTrue();
    [Fact] void should_refuse_until_every_key_is_written() => _unfinished.Errors.ShouldContain(AppendError.ConstraintIndexUnavailable);
    [Fact] void should_not_repeat_the_completed_write() => _firstWrites.ShouldEqual(1);
    [Fact] void should_continue_the_remaining_write() => _secondWrites.ShouldEqual(2);
    [Fact] void should_reuse_the_folded_differences() => _rangeReads.ShouldEqual(1);
    [Fact] void should_validate_after_completion() => _result.ConstraintViolations.ShouldNotBeEmpty();
}
