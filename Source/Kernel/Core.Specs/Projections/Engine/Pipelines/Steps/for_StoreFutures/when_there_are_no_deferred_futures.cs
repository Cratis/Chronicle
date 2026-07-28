// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_StoreFutures;

public class when_there_are_no_deferred_futures : given.a_store_futures_step
{
    async Task Because() => await _step.Perform(_projection, _context);

    [Fact] void should_not_add_any_future() => _projectionFutures.DidNotReceive().AddFuture(Arg.Any<ProjectionFuture>());
    [Fact] void should_leave_the_tracker_unchanged() => _tracker.HasPending.ShouldBeFalse();
}
