// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_resolving_a_deeper_grandchild_from_the_sink;

public class and_intermediate_keys_repeat_across_roots : given.two_roots_with_repeated_keys
{
    async Task Because() => _result = await Resolve();

    [Fact] void should_resolve_to_the_events_root() => _result.Value.ShouldEqual(SecondRoot);
    [Fact] void should_keep_all_three_collection_indexers() => _result.ArrayIndexers.All.Count().ShouldEqual(3);
    [Fact] void should_keep_the_leaf_identifier() => _result.ArrayIndexers.All.Last().Identifier.ShouldEqual(ItemKey);
    [Fact] void should_scope_the_intermediate_lookup_to_the_events_root() => Sink.Received(1).TryFindRootKeyByChildValueInRoot("features.featureId", SliceKey, Arg.Is<Key>(key => Equals(key.Value, SecondRoot)));
    [Fact] void should_not_select_an_intermediate_key_from_another_root() => Sink.DidNotReceive().TryFindRootKeyByChildValue("features.featureId", SliceKey);
}
