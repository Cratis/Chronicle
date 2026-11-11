// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.when_processing_events_live;

public class and_no_level_holds_the_great_grandchild_parent : given.a_great_grandchild_future
{
    async Task Because() => await PerformWithState(Root(Child("child-1", "grandchild-1"), Child("child-2")));

    [Fact] void should_not_resolve_the_future() => _projectionFutures.DidNotReceive().ResolveFuture(Arg.Any<ProjectionFutureId>());
    [Fact] void should_not_project_the_future() => _resolvedContext.ShouldBeNull();
    [Fact] void should_keep_the_tracker_pending() => _tracker.HasPending.ShouldBeTrue();
}
