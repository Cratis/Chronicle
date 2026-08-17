// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_identifying_read_model_key_for_keyed_child_update;

/// <summary>
/// A keyed update event for a child that was already projected under the document identified by the
/// resolved parent key must resolve its placement directly from the sink — without looking up parent
/// events or scanning for the child's creation event in the event sequence on every subsequent event
/// of the stream. This is the production scenario from issue #3723: one parent-event lookup, one
/// creation-event scan and one rejection per unrelated move/rename event, forever.
/// </summary>
public class and_child_already_exists_in_sink : given.a_flat_child_projection_with_keyed_events
{
    KeyResolverResult _result;
    PropertyPath _queriedChildPropertyPath;

    void Establish()
    {
        // The parent's own event type never occurs on this stream — the resolution used to fall
        // through to re-resolving the child's creation event on every update.
        Storage.TryGetLastInstanceOfAny(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventTypeId>>())
            .Returns(Option<AppendedEvent>.None());
        Storage.GetHeadSequenceNumber(Arg.Any<IEnumerable<EventType>>(), (EventSourceId)ChildKey)
            .Returns(EventSequenceNumber.Unavailable);
        Storage.GetHeadSequenceNumber(Arg.Any<IEnumerable<EventType>>(), (EventSourceId)BoardKey)
            .Returns(new EventSequenceNumber(5));
        var cursor = CreateCursorWith(ChildAddedEvent);
        Storage.GetRange(
                Arg.Any<EventSequenceNumber>(),
                Arg.Any<EventSequenceNumber>(),
                (EventSourceId)BoardKey,
                Arg.Any<IEnumerable<EventType>>(),
                Arg.Any<IEnumerable<Tag>?>(),
                Arg.Any<CancellationToken>())
            .Returns(cursor);

        Sink.When(x => x.TryFindRootKeyByChildValue(Arg.Any<PropertyPath>(), ChildKey))
            .Do(callInfo => _queriedChildPropertyPath = callInfo.ArgAt<PropertyPath>(0));
        Sink.TryFindRootKeyByChildValue(Arg.Any<PropertyPath>(), ChildKey)
            .Returns(new Option<Key>(new Key(BoardKey, ArrayIndexers.NoIndexers)));
        Sink.TryFindRootKeyByChildValue(Arg.Any<PropertyPath>(), BoardKey)
            .Returns(Option<Key>.None());
    }

    async Task Because() => _result = await CreateResolverUnderTest()(Storage, Sink, ChildMovedEvent);

    [Fact] void should_resolve_to_the_board_key() => (_result as ResolvedKey).Key.Value.ShouldEqual(BoardKey);
    [Fact] void should_have_array_indexer_for_the_child() => (_result as ResolvedKey).Key.ArrayIndexers.All.Single(_ => _.ArrayProperty == "children").Identifier.ShouldEqual(ChildKey);
    [Fact] void should_query_the_sink_by_the_child_key_path() => _queriedChildPropertyPath.ShouldEqual((PropertyPath)"children.childId");
    [Fact] void should_not_look_up_parent_events_in_the_event_sequence() => Storage.DidNotReceive().TryGetLastInstanceOfAny(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventTypeId>>());
    [Fact] void should_not_scan_the_event_sequence_for_creation_events() => Storage.DidNotReceive().GetHeadSequenceNumber(Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>());
}
