// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_resolving_a_deeper_grandchild_from_the_sink.given;

public class two_roots_with_repeated_keys : when_identifying_read_model_key_for_nested_event_within_grandchild_projection.given.a_three_level_hierarchy
{
    protected const string FirstRoot = "root-a";
    protected const string SecondRoot = "root-b";
    protected const string ItemKey = "item-key";
    protected IProjection ItemProjection;
    protected Key _result;

    void Establish()
    {
        ItemProjection = Substitute.For<IProjection>();
        ItemProjection.HasParent.Returns(true);
        ItemProjection.Parent.Returns(SliceProjection);
        ItemProjection.ChildrenPropertyPath.Returns((PropertyPath)"items");
        ItemProjection.IdentifiedByProperty.Returns((PropertyPath)"itemId");
        ItemProjection.Path.Returns((ProjectionPath)"items");
        Storage.WithParentHistory(SliceKey);

        // Both roots contain the same intermediate key. The unscoped lookup selects the first root.
        Sink.TryFindRootKeyByChildValue("features.featureId", SliceKey)
            .Returns(new Option<Key>(new Key(FirstRoot, ArrayIndexers.NoIndexers)));
        Sink.TryFindRootKeyByChildValueInRoot("features.slices.sliceId", SliceKey, Arg.Is<Key>(key => Equals(key.Value, SecondRoot)))
            .Returns(new Option<Key>(new Key(SecondRoot, ArrayIndexers.NoIndexers)));
        Sink.TryFindRootKeyByChildValueInRoot("features.featureId", SliceKey, Arg.Is<Key>(key => Equals(key.Value, SecondRoot)))
            .Returns(new Option<Key>(new Key(SecondRoot, ArrayIndexers.NoIndexers)));
    }

    protected async Task<Key> Resolve()
    {
        var @event = CreateEvent(4, new EventType("item-added", 1), SecondRoot, new { sliceId = SliceKey, itemId = ItemKey }.AsExpandoObject());
        var resolver = KeyResolvers.FromParentHierarchy(
            ItemProjection,
            KeyResolvers.FromEventValueProvider(EventValueProviders.EventContent("itemId")),
            KeyResolvers.FromEventValueProvider(EventValueProviders.EventContent("sliceId")),
            "itemId");
        return ((ResolvedKey)await resolver(Storage, Sink, @event)).Key;
    }
}
