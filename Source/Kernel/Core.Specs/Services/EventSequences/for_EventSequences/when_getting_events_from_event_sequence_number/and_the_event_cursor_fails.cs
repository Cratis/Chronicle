// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Services.EventSequences.for_EventSequences.when_getting_events_from_event_sequence_number;

public class and_the_event_cursor_fails : given.all_dependencies
{
    IEventCursor _cursor = null!;
    Exception _error = null!;

    void Establish()
    {
        var eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        _namespaceStorage.GetEventSequence(Arg.Any<Concepts.EventSequences.EventSequenceId>()).Returns(eventSequenceStorage);

        _cursor = Substitute.For<IEventCursor>();
        _cursor.When(_ => _.MoveNext()).Do(_ => throw new Exception("cursor failure"));
        eventSequenceStorage.GetFromSequenceNumber(
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<EventSourceId?>(),
            Arg.Any<EventStreamType?>(),
            Arg.Any<EventStreamId?>(),
            Arg.Any<IEnumerable<EventType>?>())
            .Returns(_cursor);
    }

    async Task Because() => _error = await Catch.Exception(() => _eventSequences.GetEventsFromEventSequenceNumber(new GetFromEventSequenceNumberRequest
    {
        EventStore = "event-store",
        Namespace = "event-store-namespace",
        EventSequenceId = "event-log",
        FromEventSequenceNumber = 0UL
    }));

    [Fact] void should_propagate_the_failure() => _error.ShouldNotBeNull();
    [Fact] void should_dispose_the_cursor() => _cursor.Received().Dispose();
}
