// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_resolving_a_grandchild_from_the_sink;

public class and_the_event_comes_from_a_separate_child_stream : given.two_roots_with_children
{
    void Establish()
    {
        AddRoot(FirstRoot, "another-child");
        AddRoot(SecondRoot, FeatureKey);
    }

    async Task Because() => _result = await Resolve("slice-event-source");

    [Fact] void should_keep_resolving_the_root_that_holds_the_parent() => _result.Value.ShouldEqual(SecondRoot);
}
