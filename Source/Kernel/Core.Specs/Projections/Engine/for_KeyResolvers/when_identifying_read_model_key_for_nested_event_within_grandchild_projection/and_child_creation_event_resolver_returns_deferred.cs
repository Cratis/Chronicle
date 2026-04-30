// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_identifying_read_model_key_for_nested_event_within_grandchild_projection;

public class and_child_creation_event_resolver_returns_deferred : given.a_three_level_hierarchy
{
    void Establish()
    {
        Storage.GetHeadSequenceNumber(Arg.Any<IEnumerable<EventType>>(), (EventSourceId)SliceKey)
            .Returns(new EventSequenceNumber(2));
        Storage.GetRange(
                Arg.Any<EventSequenceNumber>(),
                Arg.Any<EventSequenceNumber>(),
                (EventSourceId)SliceKey,
                Arg.Any<IEnumerable<EventType>>(),
                Arg.Any<CancellationToken>())
            .Returns(CreateCursorWith(SliceAddedEvent));

        // Override: the key resolver for SliceAddedEventType returns a deferred result,
        // so TryResolveViaChildCreationEvent cannot resolve and returns null.
        var deferredFuture = new ProjectionFuture(
            new ProjectionFutureId(Guid.NewGuid()),
            new ProjectionId("test-projection"),
            SliceAddedEvent,
            PropertyPath.NotSet,
            PropertyPath.NotSet,
            PropertyPath.NotSet,
            PropertyPath.NotSet,
            new Key(SliceKey, ArrayIndexers.NoIndexers),
            DateTimeOffset.UtcNow);

        FeatureProjection.GetKeyResolverFor(SliceAddedEventType).Returns(
            (_, __, ___) => Task.FromResult(KeyResolverResult.Deferred(deferredFuture)));

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
