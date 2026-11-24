// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_resolving_a_grandchild_from_the_sink;

public class and_child_keys_are_unique : given.two_roots_with_children
{
    void Establish()
    {
        AddRoot(FirstRoot, "another-child");
        AddRoot(SecondRoot, FeatureKey);
    }

    async Task Because() => _result = await Resolve(SecondRoot);

    [Fact] void should_resolve_the_events_root() => _result.Value.ShouldEqual(SecondRoot);
}
