// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.InMemory.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_after_a_failed_constraint_index_update;

public class and_the_repair_stops_at_the_budget : given.an_event_sequence_with_repairable_indexes
{
    readonly given.RepairClock _clock = new();
    readonly TaskCompletionSource _reading = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<bool> _page = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly List<EventSequenceNumber> _starts = [];
    AppendResult _stopped;
    protected override EventSequenceNumber InitialNext => 2;

    async Task Establish()
    {
        await Seed(0);
        await Seed(1);
        _eventSequence.ConstraintIndexTimeProvider = _clock;
        var cursor = Substitute.For<IEventCursor>();
        cursor.Current.Returns(_ => _log.Events.Take(1));
        var pages = 0;
        cursor.MoveNext().Returns(_ => ++pages == 1 ? Task.FromResult(true) : Block());
        _eventSequenceStorage.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var start = call.ArgAt<EventSequenceNumber>(0);
                _starts.Add(start);
                return _starts.Count == 1 ? cursor : new EventCursor(_log.Events.Where(e => e.Context.SequenceNumber >= start).ToList());
            });
    }

    Task<bool> Block()
    {
        _reading.TrySetResult();
        return _page.Task;
    }

    async Task Because()
    {
        _eventSourceId = EventSourceId.New();
        var append = AppendAnEvent();
        await _reading.Task.WaitAsync(TimeSpan.FromSeconds(2), TimeProvider.System);
        _clock.Advance(TimeSpan.FromSeconds(5));
        _page.TrySetCanceled();
        _stopped = await append.WaitAsync(TimeSpan.FromSeconds(2), TimeProvider.System);
        _clock.Advance(TimeSpan.FromSeconds(5));
        _result = await AppendAnEvent();
    }

    [Fact] void should_refuse_the_unfinished_turn() => _stopped.Errors.ShouldContain(AppendError.ConstraintIndexUnavailable);
    [Fact] void should_resume_after_the_completed_page() => _starts.Select(n => n.Value).ShouldEqual(new ulong[] { 0, 1 });
    [Fact] void should_validate_against_the_completed_repair() => _result.ConstraintViolations.ShouldNotBeEmpty();
}
