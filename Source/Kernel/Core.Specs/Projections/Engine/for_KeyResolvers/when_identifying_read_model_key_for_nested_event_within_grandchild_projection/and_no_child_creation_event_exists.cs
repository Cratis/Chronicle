// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_identifying_read_model_key_for_nested_event_within_grandchild_projection;

public class and_no_child_creation_event_exists : given.a_three_level_hierarchy
{
    void Establish()
    {
        // No creation event found for SliceKey — TryResolveViaChildCreationEvent returns null
        // and the code falls through to the sink lookup.
        Storage.GetHeadSequenceNumber(Arg.Any<IEnumerable<EventType>>(), (EventSourceId)SliceKey)
            .Returns(EventSequenceNumber.Unavailable);

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
