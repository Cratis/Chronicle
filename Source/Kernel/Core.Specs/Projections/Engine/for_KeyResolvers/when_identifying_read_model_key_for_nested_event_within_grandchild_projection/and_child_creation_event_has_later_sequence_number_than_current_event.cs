// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_identifying_read_model_key_for_nested_event_within_grandchild_projection;

/// <summary>
/// When the candidate "child creation event" is at a later sequence number than the event being resolved,
/// the cycle-detection guard must reject it and fall through to the sink lookup.
/// A creation event that comes after the current event cannot be the anchor that establishes parentage
/// for the current event; accepting it produces an infinite A→B→A resolution cycle.
/// </summary>
public class and_child_creation_event_has_later_sequence_number_than_current_event : given.a_three_level_hierarchy
{
    void Establish()
    {
        // Create a SliceAddedEvent at seq 5 — later than NestedCommandEvent (seq 3).
        var creationEventAfterCurrentEvent = CreateEvent(5, SliceAddedEventType, SliceKey, new System.Dynamic.ExpandoObject());

        Storage.GetHeadSequenceNumber(Arg.Any<IEnumerable<EventType>>(), (EventSourceId)SliceKey)
            .Returns(new EventSequenceNumber(5));
        var cursor = CreateCursorWith(creationEventAfterCurrentEvent);
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
