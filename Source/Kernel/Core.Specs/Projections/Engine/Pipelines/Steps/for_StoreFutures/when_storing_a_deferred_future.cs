// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_StoreFutures;

public class when_storing_a_deferred_future : given.a_store_futures_step
{
    void Establish() => _context.AddDeferredFuture(CreateFuture());

    async Task Because() => await _step.Perform(_projection, _context);

    [Fact] void should_add_the_future_to_the_grain() => _projectionFutures.Received().AddFuture(Arg.Any<ProjectionFuture>());
    [Fact] void should_mark_the_tracker_pending() => _tracker.HasPending.ShouldBeTrue();
}
