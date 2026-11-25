// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.when_processing_events_live;

public class and_a_great_grandchild_parent_is_under_a_later_sibling : given.a_great_grandchild_future
{
    async Task Because() => await PerformWithState(Root(Child("child-1", "grandchild-1"), Child("child-2", "grandchild-2")));

    [Fact] void should_resolve_the_future() => _projectionFutures.Received(1).ResolveFuture(_future.Id);
    [Fact] void should_index_both_ancestors_and_the_child() => _resolvedContext!.Key.ArrayIndexers.All.Count().ShouldEqual(3);
    [Fact] void should_index_the_sibling_that_holds_the_parent() => _resolvedContext!.Key.ArrayIndexers.All.First().Identifier.ShouldEqual("child-2");
    [Fact] void should_index_the_parent() => _resolvedContext!.Key.ArrayIndexers.All.ElementAt(1).Identifier.ShouldEqual("grandchild-2");
}
