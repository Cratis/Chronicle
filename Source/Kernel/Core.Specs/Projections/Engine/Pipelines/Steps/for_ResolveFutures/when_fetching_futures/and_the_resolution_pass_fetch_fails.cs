// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.when_fetching_futures;

public class and_the_resolution_pass_fetch_fails : given.a_resolve_futures_step
{
    ProjectionEventContext _result;
    Exception? _exception;

    void Establish() => _projectionFutures.GetFutures().Returns(
        Task.FromResult<IEnumerable<ProjectionFuture>>([CreateFuture()]),
        Task.FromException<IEnumerable<ProjectionFuture>>(new InvalidOperationException()));

    async Task Because() => _exception = await Catch.Exception(async () => _result = await _step.Perform(_projection, _context));

    [Fact] void should_not_fail_the_event() => _exception.ShouldBeNull();
    [Fact] void should_keep_the_event_context_unchanged() => _result.ShouldEqual(_context);
    [Fact] void should_leave_futures_pending_for_retry() => _tracker.HasPending.ShouldBeTrue();
    [Fact] void should_not_resolve_any_future() => _projectionFutures.DidNotReceive().ResolveFuture(Arg.Any<ProjectionFutureId>());
}
