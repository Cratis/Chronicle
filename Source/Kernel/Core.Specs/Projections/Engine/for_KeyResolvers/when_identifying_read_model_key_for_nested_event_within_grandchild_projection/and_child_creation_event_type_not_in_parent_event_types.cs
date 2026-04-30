// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_identifying_read_model_key_for_nested_event_within_grandchild_projection;

public class and_child_creation_event_type_not_in_parent_event_types : given.a_three_level_hierarchy
{
    void Establish()
    {
        // Override: SliceAddedEventType is NOT in FeatureProjection's event types.
        // TryResolveViaChildCreationEvent finds the creation event but cannot look up
        // a key resolver for it in the parent projection, so it returns null.
        FeatureProjection.EventTypes.Returns([FeatureAddedEventType]);

        Storage.GetHeadSequenceNumber(Arg.Any<IEnumerable<EventType>>(), (EventSourceId)SliceKey)
            .Returns(new EventSequenceNumber(2));
        Storage.GetRange(
                Arg.Any<EventSequenceNumber>(),
                Arg.Any<EventSequenceNumber>(),
                (EventSourceId)SliceKey,
                Arg.Any<IEnumerable<EventType>>(),
                Arg.Any<CancellationToken>())
            .Returns(CreateCursorWith(SliceAddedEvent));

        Sink.TryFindRootKeyByChildValue(Arg.Any<PropertyPath>(), Arg.Any<object>())
            .Returns(Option<Key>.None());
    }

    async Task Because() =>
        await KeyResolvers.FromParentHierarchy(
            SliceProjection,
            KeyResolvers.FromEventSourceId,
            KeyResolvers.FromEventSourceId,
            "sliceId")(Storage, Sink, NestedCommandEvent);

    [Fact] void should_fall_through_to_sink_lookup() =>
        Sink.Received().TryFindRootKeyByChildValue(Arg.Any<PropertyPath>(), Arg.Any<object>());
}
