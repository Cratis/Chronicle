// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_resolving_a_grandchild_from_the_sink;

public class and_child_keys_repeat_across_roots : given.two_roots_with_children
{
    Key _first;

    void Establish()
    {
        AddRoot(FirstRoot, FeatureKey);
        AddRoot(SecondRoot, FeatureKey);
    }

    async Task Because()
    {
        _first = await Resolve(FirstRoot);
        _result = await Resolve(SecondRoot);
    }

    [Fact] void should_resolve_the_first_events_own_root() => _first.Value.ShouldEqual(FirstRoot);
    [Fact] void should_resolve_the_second_events_own_root() => _result.Value.ShouldEqual(SecondRoot);
    [Fact] void should_keep_the_parent_child_indexer() => _result.ArrayIndexers.All.First().Identifier.ShouldEqual(FeatureKey);
    [Fact] void should_keep_the_grandchild_indexer() => _result.ArrayIndexers.All.Last().Identifier.ShouldEqual(SliceKey);
}
