// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.InMemory;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_individual_reads_recover_after_a_page_fault : given.a_replay
{
    int _pages;
    int _pointReads;

    void Establish()
    {
        Event(0, "first", "Claimed", "first");
        Event(1, "second", "Claimed", "second");
        Event(2, "third", "Claimed", "third");
        var broken = Substitute.For<IEventCursor>();
        broken.MoveNext().Returns(Task.FromException<bool>(new FormatException("bad page")));
        _sequence.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>())
            .Returns(call => ++_pages == 1 ? broken : new Storage.InMemory.EventSequences.EventCursor(_events.Where(e => e.Context.SequenceNumber >= call.ArgAt<EventSequenceNumber>(0)).ToList()));
        _sequence.GetNextSequenceNumberGreaterOrEqualThan(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<EventSourceId?>())
            .Returns(call => Task.FromResult(_events.Where(e => e.Context.SequenceNumber >= call.ArgAt<EventSequenceNumber>(0)).Select(e => e.Context.SequenceNumber).DefaultIfEmpty(EventSequenceNumber.Unavailable).First()));
        _sequence.GetEventAt(Arg.Any<EventSequenceNumber>()).Returns(call =>
        {
            _pointReads++;
            return Task.FromResult(_events.Single(e => e.Context.SequenceNumber == call.ArgAt<EventSequenceNumber>(0)));
        });
        Start();
        _replay.StorageErrorClassifier = new InMemoryStorageErrorClassifier();
    }

    async Task Because()
    {
        await _replay.Continue();
        _result = await _replay.Continue();
    }

    [Fact] void should_return_to_batched_reads() => _pages.ShouldEqual(2);
    [Fact] void should_only_point_read_the_faulted_slot() => _pointReads.ShouldEqual(1);
    [Fact] void should_complete_the_recovered_fold() => _result.IsCompleted.ShouldBeTrue();
    [Fact] async Task should_preserve_the_remaining_events() => (await Values("third")).ShouldContain(Hash("third").Value);
}
