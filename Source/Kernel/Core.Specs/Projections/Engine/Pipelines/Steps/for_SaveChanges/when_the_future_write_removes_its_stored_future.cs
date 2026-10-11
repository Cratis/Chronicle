// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges;

public class when_the_future_write_removes_its_stored_future : given.a_stored_pending_future
{
    IEnumerable<ProjectionFuture> _before;
    IEnumerable<ProjectionFuture> _after;

    async Task Because()
    {
        _before = await _futureStorage.GetForProjection(_projection.Identifier);
        _result = await _step.Perform(_projection, _context);
        _after = await _futureStorage.GetForProjection(_projection.Identifier);
    }

    [Fact] void should_have_the_future_before_saving() => _before.Select(future => future.Id).ShouldContainOnly(_futureId);
    [Fact] void should_remove_the_future_from_storage_after_saving() => _after.ShouldBeEmpty();
    [Fact] void should_not_report_failures() => _result.FailedPartitions.ShouldBeEmpty();
}
