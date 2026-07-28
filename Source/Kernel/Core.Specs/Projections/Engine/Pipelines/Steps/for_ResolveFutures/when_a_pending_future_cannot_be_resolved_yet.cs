// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures;

public class when_a_pending_future_cannot_be_resolved_yet : given.a_resolve_futures_step
{
    ProjectionFuture _future;

    void Establish()
    {
        _future = CreateFuture();
        _projectionFutures.GetFutures().Returns(Task.FromResult<IEnumerable<ProjectionFuture>>([_future]));
    }

    async Task Because() => await _step.Perform(_projection, _context);

    [Fact] void should_probe_the_futures_grain() => _projectionFutures.Received().GetFutures();
    [Fact] void should_not_resolve_the_future() => _projectionFutures.DidNotReceive().ResolveFuture(Arg.Any<ProjectionFutureId>());
    [Fact] void should_keep_the_tracker_pending() => _tracker.HasPending.ShouldBeTrue();
}
