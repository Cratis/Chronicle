// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.InMemory.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_repairing_constraint_indexes_on_the_timer;

public class and_a_transient_cursor_recovers : given.an_event_sequence_with_repairable_indexes
{
    readonly given.RepairClock _clock = new();
    int _reads;
    protected override EventSequenceNumber InitialNext => 1;

    async Task Establish()
    {
        await Seed(0);
        _eventSequence.ConstraintIndexTimeProvider = _clock;
        var failed = Substitute.For<IEventCursor>();
        failed.MoveNext().Returns(Task.FromException<bool>(new TimeoutException()));
        _eventSequenceStorage.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>())
            .Returns(_ => ++_reads == 1 ? failed : new EventCursor(_log.Events.ToList()));
        await given.ConstraintRepairTimer.Tick(_silo);
        _clock.Advance(TimeSpan.FromSeconds(1));
    }

    Task Because() => given.ConstraintRepairTimer.Tick(_silo);

    [Fact] void should_resume_batched_reads_after_a_transient_fault() => _reads.ShouldEqual(2);
    [Fact] async Task should_repair_after_the_cursor_recovers() => (await _claims.GetFor(_eventSourceId, _uniqueDefinition)).ShouldNotBeEmpty();
}
