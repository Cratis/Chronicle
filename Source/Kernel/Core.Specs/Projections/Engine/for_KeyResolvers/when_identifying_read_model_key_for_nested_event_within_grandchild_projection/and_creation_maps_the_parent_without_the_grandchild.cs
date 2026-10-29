// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_identifying_read_model_key_for_nested_event_within_grandchild_projection;

public class and_creation_maps_the_parent_without_the_grandchild : given.a_three_level_hierarchy
{
    Key _result;
    Key _parentKey;

    void Establish()
    {
        Storage.GetHeadSequenceNumber(Arg.Any<IEnumerable<EventType>>(), (EventSourceId)SliceKey)
            .Returns(new EventSequenceNumber(2));
        var cursor = CreateCursorWith(SliceAddedEvent);
        Storage.GetRange(
                Arg.Any<EventSequenceNumber>(),
                Arg.Any<EventSequenceNumber>(),
                (EventSourceId)SliceKey,
                Arg.Any<IEnumerable<EventType>>(),
                Arg.Any<IEnumerable<Tag>?>(),
                Arg.Any<CancellationToken>())
            .Returns(cursor);

        _parentKey = new Key(RootKey, new ArrayIndexers([new ArrayIndexer("features", "featureId", FeatureKey)]));
        FeatureProjection.GetKeyResolverFor(SliceAddedEventType).Returns(
            (_, __, ___) => Task.FromResult(KeyResolverResult.Resolved(_parentKey)));
    }

    async Task Because()
    {
        var result = await KeyResolvers.FromParentHierarchy(
            SliceProjection,
            KeyResolvers.FromEventSourceId,
            KeyResolvers.FromEventSourceId,
            "sliceId")(Storage, Sink, NestedCommandEvent);
        _result = ((ResolvedKey)result).Key;
    }

    [Fact] void should_preserve_the_root_key() => _result.Value.ShouldEqual(RootKey);
    [Fact] void should_preserve_the_ancestor_indexer() => _result.ArrayIndexers.GetFor("features").Identifier.ToString().ShouldEqual(FeatureKey);
    [Fact] void should_preserve_the_ancestor_identifier_property() => _result.ArrayIndexers.GetFor("features").IdentifierProperty.ShouldEqual((PropertyPath)"featureId");
    [Fact] void should_add_the_grandchild_indexer() => _result.ArrayIndexers.GetFor("slices").Identifier.ToString().ShouldEqual(SliceKey);
    [Fact] void should_identify_the_grandchild_by_its_identity() => _result.ArrayIndexers.GetFor("slices").IdentifierProperty.ShouldEqual((PropertyPath)"sliceId");
    [Fact] void should_have_exactly_two_indexers() => _result.ArrayIndexers.Count.ShouldEqual(2);
    [Fact] void should_not_modify_the_parent_key() => _parentKey.ArrayIndexers.Count.ShouldEqual(1);
}
