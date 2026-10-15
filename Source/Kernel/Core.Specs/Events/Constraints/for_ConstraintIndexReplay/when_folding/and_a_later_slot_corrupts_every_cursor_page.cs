// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.InMemory;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_later_slot_corrupts_every_cursor_page : given.a_replay
{
    readonly ConstraintIndexPoisonTracker _tracker = new(new InMemoryStorageErrorClassifier());
    int _pageFaults;
    int _poisonReads;

    void Establish()
    {
        for (ulong number = 0; number < 6; number++) Event(number, $"source-{number}", "Claimed", $"value-{number}");
        _sequence.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var from = call.ArgAt<EventSequenceNumber>(0);
                if (from > (EventSequenceNumber)2) return new Storage.InMemory.EventSequences.EventCursor(_events.Where(e => e.Context.SequenceNumber >= from).ToList());
                var broken = Substitute.For<IEventCursor>();
                broken.MoveNext().Returns(_ =>
                {
                    _pageFaults++;
                    return Task.FromException<bool>(new FormatException("page contains corrupt slot 2"));
                });
                return broken;
            });
        _sequence.GetNextSequenceNumberGreaterOrEqualThan(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<EventSourceId?>())
            .Returns(call => Task.FromResult(_events.Where(e => e.Context.SequenceNumber >= call.ArgAt<EventSequenceNumber>(0)).Select(e => e.Context.SequenceNumber).DefaultIfEmpty(EventSequenceNumber.Unavailable).First()));
        _sequence.GetEventAt(Arg.Any<EventSequenceNumber>()).Returns(call =>
        {
            var position = call.ArgAt<EventSequenceNumber>(0);
            if (position != (EventSequenceNumber)2) return Task.FromResult(_events.Single(e => e.Context.SequenceNumber == position));
            _poisonReads++;
            return Task.FromException<AppendedEvent>(new FormatException("corrupt slot 2"));
        });
        Start(cap: 4);
        _replay.StorageErrorClassifier = new InMemoryStorageErrorClassifier();
    }

    async Task Because()
    {
        for (var turn = 0; turn < 40; turn++)
        {
            _result = await _replay.Continue();
            foreach (var (name, failure) in _tracker.Observe(_result.Failed, _replay.StorageCallSucceeded)) _replay.SettlePoison(name, failure.Position!);
            if (_result.IsCompleted) break;
        }
    }

    [Fact] void should_fault_the_page_only_once_across_poison_refolding_and_segments() => _pageFaults.ShouldEqual(1);
    [Fact] void should_attribute_five_consecutive_failures_to_the_corrupt_slot() => _poisonReads.ShouldEqual(5);
    [Fact] void should_exercise_multiple_segment_passes() => _replay.SegmentCount.ShouldBeGreaterThan(1);
    [Fact] void should_complete_the_fold() => _result.IsCompleted.ShouldBeTrue();
    [Fact] async Task should_preserve_the_last_healthy_event() => (await Values("source-5")).ShouldContain(Hash("value-5").Value);
}
