// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures;

public class when_tracker_pending_but_store_is_empty : given.a_resolve_futures_step
{
    ProjectionEventContext _result;

    async Task Because() => _result = await _step.Perform(_projection, _context);

    [Fact] void should_probe_the_futures_grain_once() => _projectionFutures.Received(1).GetFutures();
    [Fact] void should_clear_the_tracker() => _tracker.HasPending.ShouldBeFalse();
    [Fact] void should_return_the_same_context() => _result.ShouldEqual(_context);
}
