// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures;

public class when_the_event_itself_was_deferred : given.a_resolve_futures_step
{
    ProjectionEventContext _result;

    void Establish() => _context.AddDeferredFuture(CreateFuture());

    async Task Because() => _result = await _step.Perform(_projection, _context);

    [Fact] void should_not_call_the_futures_grain() => _projectionFutures.DidNotReceive().GetFutures();
    [Fact] void should_return_the_same_context() => _result.ShouldEqual(_context);
}
