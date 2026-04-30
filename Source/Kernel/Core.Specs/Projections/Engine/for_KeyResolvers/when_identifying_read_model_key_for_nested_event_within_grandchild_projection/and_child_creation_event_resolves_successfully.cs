// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_identifying_read_model_key_for_nested_event_within_grandchild_projection;

public class and_child_creation_event_resolves_successfully : given.a_three_level_hierarchy
{
    Key _result;

    void Establish()
    {
        // SliceAddedEvent is found as the head event for SliceKey (the child creation event).
        Storage.GetHeadSequenceNumber(Arg.Any<IEnumerable<EventType>>(), (EventSourceId)SliceKey)
            .Returns(new EventSequenceNumber(2));
        Storage.GetRange(
                Arg.Any<EventSequenceNumber>(),
                Arg.Any<EventSequenceNumber>(),
                (EventSourceId)SliceKey,
                Arg.Any<IEnumerable<EventType>>(),
                Arg.Any<CancellationToken>())
            .Returns(CreateCursorWith(SliceAddedEvent));

        // Inner resolution chain: finding FeatureAdded for FeatureKey and ModuleAdded for RootKey.
        Storage.TryGetLastInstanceOfAny(FeatureKey, Arg.Any<IEnumerable<EventTypeId>>())
            .Returns(new Option<AppendedEvent>(FeatureAddedEvent));
        Storage.TryGetLastInstanceOfAny(RootKey, Arg.Any<IEnumerable<EventTypeId>>())
            .Returns(new Option<AppendedEvent>(ModuleAddedEvent));
    }

    async Task Because()
    {
        var keyResult = await KeyResolvers.FromParentHierarchy(
            SliceProjection,
            KeyResolvers.FromEventSourceId,
            KeyResolvers.FromEventSourceId,
            "sliceId")(Storage, Sink, NestedCommandEvent);
        _result = (keyResult as ResolvedKey)!.Key;
    }

    [Fact] void should_return_root_document_key() => _result.Value.ShouldEqual(RootKey);
    [Fact] void should_have_feature_array_indexer() => _result.ArrayIndexers.All.Single(_ => _.ArrayProperty == "features").Identifier.ToString().ShouldEqual(FeatureKey);
    [Fact] void should_have_slice_array_indexer() => _result.ArrayIndexers.All.Single(_ => _.ArrayProperty == "slices").Identifier.ToString().ShouldEqual(SliceKey);
}
