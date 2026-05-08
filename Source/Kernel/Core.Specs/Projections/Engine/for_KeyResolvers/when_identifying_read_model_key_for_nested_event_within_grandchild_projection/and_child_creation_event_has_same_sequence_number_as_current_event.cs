// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_identifying_read_model_key_for_nested_event_within_grandchild_projection;

/// <summary>
/// When the only "child creation event" in the event sequence has the same sequence number as the
/// event being resolved, the cycle-detection guard must reject it and fall through to the sink lookup.
/// Accepting an event at the same position would indicate we are processing the creation event itself
/// as its own resolver anchor — an infinite cycle.
/// </summary>
public class and_child_creation_event_has_same_sequence_number_as_current_event : given.a_three_level_hierarchy
{
    void Establish()
    {
        // Create a SliceAddedEvent at the same sequence number as NestedCommandEvent (seq 3).
        var creationEventAtSameSequence = CreateEvent(3, SliceAddedEventType, SliceKey, new System.Dynamic.ExpandoObject());

        Storage.GetHeadSequenceNumber(Arg.Any<IEnumerable<EventType>>(), (EventSourceId)SliceKey)
            .Returns(new EventSequenceNumber(3));
        var cursor = CreateCursorWith(creationEventAtSameSequence);
        Storage.GetRange(
                Arg.Any<EventSequenceNumber>(),
                Arg.Any<EventSequenceNumber>(),
                (EventSourceId)SliceKey,
                Arg.Any<IEnumerable<EventType>>(),
                Arg.Any<CancellationToken>())
            .Returns(cursor);

        Sink.TryFindRootKeyByChildValue(Arg.Any<PropertyPath>(), Arg.Any<object>())
            .Returns(Option<Key>.None());
    }

    async Task Because() =>
        await KeyResolvers.FromParentHierarchy(
            SliceProjection,
            KeyResolvers.FromEventSourceId,
            KeyResolvers.FromEventSourceId,
            "sliceId")(Storage, Sink, NestedCommandEvent);

    [Fact] void should_not_use_the_creation_event_as_an_anchor() =>
        Sink.Received().TryFindRootKeyByChildValue(Arg.Any<PropertyPath>(), Arg.Any<object>());
}
