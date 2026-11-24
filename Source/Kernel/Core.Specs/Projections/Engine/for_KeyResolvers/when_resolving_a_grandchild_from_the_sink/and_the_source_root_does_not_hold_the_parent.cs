// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_resolving_a_grandchild_from_the_sink;

public class and_the_source_root_does_not_hold_the_parent : given.two_roots_with_children
{
    void Establish()
    {
        AddRoot(FirstRoot, FeatureKey);
        AddRoot(SecondRoot, "another-child");
    }

    async Task Because() => _result = await Resolve(SecondRoot);

    [Fact] void should_keep_the_unscoped_fallback_when_root_identity_is_not_established() => _result.Value.ShouldEqual(FirstRoot);
}
