// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges;

public class when_the_pending_future_write_succeeds : given.a_pending_future_save
{
    async Task Because() => _result = await _step.Perform(_projection, _context);

    [Fact] void should_resolve_the_future_after_writing() => _operations.ShouldEqual(["write", "resolve"]);
    [Fact] void should_remove_the_persisted_future_once() => _futures.Received(1).ResolveFuture(_futureId);
    [Fact] void should_not_report_failures() => _result.FailedPartitions.ShouldBeEmpty();
}
