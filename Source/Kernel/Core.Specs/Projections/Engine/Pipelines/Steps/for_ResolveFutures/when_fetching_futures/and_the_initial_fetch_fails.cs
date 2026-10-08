// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.when_fetching_futures;

public class and_the_initial_fetch_fails : given.a_resolve_futures_step
{
    Exception _fetchFailure;
    Exception? _exception;

    void Establish()
    {
        _fetchFailure = new InvalidOperationException();
        _projectionFutures.GetFutures().Returns(Task.FromException<IEnumerable<ProjectionFuture>>(_fetchFailure));
    }

    async Task Because() => _exception = await Catch.Exception(async () => await _step.Perform(_projection, _context));

    [Fact] void should_propagate_the_fetch_failure() => _exception.ShouldEqual(_fetchFailure);
}
