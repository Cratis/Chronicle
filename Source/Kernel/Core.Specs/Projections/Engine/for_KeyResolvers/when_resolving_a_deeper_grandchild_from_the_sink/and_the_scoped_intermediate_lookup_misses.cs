// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_resolving_a_deeper_grandchild_from_the_sink;

public class and_the_scoped_intermediate_lookup_misses : given.two_roots_with_repeated_keys
{
    void Establish() => Sink.TryFindRootKeyByChildValueInRoot("features.featureId", SliceKey, Arg.Any<Key>()).Returns(Option<Key>.None());

    async Task Because() => _result = await Resolve();

    [Fact] void should_keep_the_resolved_root() => _result.Value.ShouldEqual(SecondRoot);
    [Fact] void should_keep_all_three_collection_indexers() => _result.ArrayIndexers.All.Count().ShouldEqual(3);
    [Fact] void should_try_the_events_root_first() => Sink.Received(1).TryFindRootKeyByChildValueInRoot("features.featureId", SliceKey, Arg.Is<Key>(key => Equals(key.Value, SecondRoot)));
    [Fact] void should_fall_back_to_the_unscoped_intermediate_lookup() => Sink.Received(1).TryFindRootKeyByChildValue("features.featureId", SliceKey);
}
