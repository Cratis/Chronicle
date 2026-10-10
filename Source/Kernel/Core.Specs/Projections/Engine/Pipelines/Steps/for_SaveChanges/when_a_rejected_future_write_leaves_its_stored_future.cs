// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges;

public class when_a_rejected_future_write_leaves_its_stored_future : given.a_stored_pending_future
{
    IEnumerable<ProjectionFuture> _remaining;
    FailedPartition _failure;

    void Establish()
    {
        _failure = new FailedPartition(_futureKey, _context.EventSequenceNumber);
        _writingSink.ApplyChanges(_futureKey, _futureChangeset, _context.EventSequenceNumber).Returns(Task.FromResult<IEnumerable<FailedPartition>>([_failure]));
    }

    async Task Because()
    {
        _result = await _step.Perform(_projection, _context);
        _remaining = await _futureStorage.GetForProjection(_projection.Identifier);
    }

    [Fact] void should_keep_the_future_in_storage() => _remaining.Select(future => future.Id).ShouldContainOnly(_futureId);
    [Fact] void should_report_the_failed_partition() => _result.FailedPartitions.ShouldContainOnly(_failure);
}
