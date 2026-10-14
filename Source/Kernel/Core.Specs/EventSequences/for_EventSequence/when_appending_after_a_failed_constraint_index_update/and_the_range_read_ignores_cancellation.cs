// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_after_a_failed_constraint_index_update;

public class and_the_range_read_ignores_cancellation : given.an_event_sequence_with_repairable_indexes
{
    readonly TaskCompletionSource<IEventCursor> _read = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly given.TriggeredRepairDeadline _deadline = new();
    Exception? _error;

    async Task Establish()
    {
        _failWrites = true;
        await AppendAnEvent();
        _eventSequence.ConstraintIndexTimeProvider = _deadline;
        _eventSequenceStorage.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _deadline.Expire();
                return _read.Task;
            });
    }
    async Task Because() => _error = await Catch.Exception(async () => _result = await AppendAnEvent().WaitAsync(TimeSpan.FromSeconds(1), TimeProvider.System));
    void Destroy() => _read.TrySetCanceled();

    [Fact] void should_stop_waiting_at_the_repair_deadline() => _error.ShouldBeNull();
    [Fact] void should_refuse_instead_of_writing_on_unknown_state() => _result.Errors.ShouldContain(AppendError.ConstraintIndexUnavailable);
}
