// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_resolving_a_deeper_grandchild_from_the_sink;

public class and_another_intermediate_level_has_repeated_keys : given.two_roots_with_repeated_keys
{
    void Establish()
    {
        var epicProjection = Substitute.For<IProjection>();
        epicProjection.HasParent.Returns(true);
        epicProjection.Parent.Returns(RootProjection);
        epicProjection.ChildrenPropertyPath.Returns((PropertyPath)"epics");
        epicProjection.IdentifiedByProperty.Returns((PropertyPath)"epicId");
        epicProjection.Path.Returns((ProjectionPath)"epics");
        FeatureProjection.Parent.Returns(epicProjection);

        Sink.TryFindRootKeyByChildValueInRoot("epics.features.slices.sliceId", SliceKey, Arg.Is<Key>(key => Equals(key.Value, SecondRoot)))
            .Returns(new Option<Key>(new Key(SecondRoot, ArrayIndexers.NoIndexers)));
        Sink.TryFindRootKeyByChildValueInRoot("epics.features.featureId", SliceKey, Arg.Is<Key>(key => Equals(key.Value, SecondRoot)))
            .Returns(new Option<Key>(new Key(SecondRoot, ArrayIndexers.NoIndexers)));
        Sink.TryFindRootKeyByChildValueInRoot("epics.epicId", SecondRoot, Arg.Is<Key>(key => Equals(key.Value, SecondRoot)))
            .Returns(new Option<Key>(new Key(SecondRoot, ArrayIndexers.NoIndexers)));
        Sink.TryFindRootKeyByChildValue("epics.features.featureId", SliceKey)
            .Returns(new Option<Key>(new Key(FirstRoot, ArrayIndexers.NoIndexers)));
        Sink.TryFindRootKeyByChildValue("epics.epicId", SecondRoot)
            .Returns(new Option<Key>(new Key(FirstRoot, ArrayIndexers.NoIndexers)));
    }

    async Task Because() => _result = await Resolve();

    [Fact] void should_keep_all_four_collection_indexers() => _result.ArrayIndexers.All.Count().ShouldEqual(4);
    [Fact] void should_not_take_the_outermost_indexer_from_a_different_root() => _result.ArrayIndexers.All.First().Identifier.ShouldEqual(SecondRoot);
    [Fact] void should_scope_every_intermediate_lookup_to_the_events_root() => Sink.Received(1).TryFindRootKeyByChildValueInRoot("epics.epicId", SecondRoot, Arg.Is<Key>(key => Equals(key.Value, SecondRoot)));
}
