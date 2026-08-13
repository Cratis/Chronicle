// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.EventSequences.for_MaterializedEventCursor;

public class when_moving_through_a_page : Specification
{
    static readonly AppendedEvent _first = AppendedEventFor(2);
    static readonly AppendedEvent _second = AppendedEventFor(1);

    MaterializedEventCursor _cursor;
    bool _firstMove;
    IEnumerable<AppendedEvent> _page;
    bool _secondMove;

    void Establish() => _cursor = new([_first, _second]);

    async Task Because()
    {
        _firstMove = await _cursor.MoveNext();
        _page = _cursor.Current;
        _secondMove = await _cursor.MoveNext();
    }

    [Fact] void should_move_to_the_page() => _firstMove.ShouldBeTrue();
    [Fact] void should_yield_every_event() => _page.Count().ShouldEqual(2);
    [Fact] void should_preserve_the_order_it_was_given() => _page.ToArray()[0].ShouldEqual(_first);
    [Fact] void should_not_move_a_second_time() => _secondMove.ShouldBeFalse();
    [Fact] void should_have_no_current_events_after_the_page() => _cursor.Current.ShouldBeEmpty();

    static AppendedEvent AppendedEventFor(ulong sequenceNumber) =>
        new(
            new EventContext(
                new EventType("TheEventType", EventTypeGeneration.First),
                EventSourceType.Default,
                "the-source",
                EventStreamType.All,
                EventStreamId.Default,
                sequenceNumber,
                DateTimeOffset.UtcNow,
                EventStoreName.NotSet,
                EventStoreNamespaceName.NotSet,
                CorrelationId.NotSet,
                [],
                Identity.System,
                [],
                EventHash.NotSet,
                EventObservationState.None,
                Subject.NotSet),
            new System.Dynamic.ExpandoObject());
}
