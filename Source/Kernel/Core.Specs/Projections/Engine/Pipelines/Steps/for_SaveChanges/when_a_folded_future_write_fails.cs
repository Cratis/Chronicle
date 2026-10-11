// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges;

public class when_a_folded_future_write_fails : given.a_pending_future_save
{
    FailedPartition _failure;

    protected override bool FoldIntoMainWrite => true;

    void Establish()
    {
        _context.Changeset.HasChanges.Returns(true);
        _failure = new FailedPartition(_context.Key, _context.EventSequenceNumber);
        _writingSink.ApplyChanges(_context.Key, _context.Changeset, _context.EventSequenceNumber, SinkWriteMode.OnlyWhenAdvancingWatermark).Returns(Task.FromResult<IEnumerable<FailedPartition>>([_failure]));
    }

    async Task Because() => _result = await _step.Perform(_projection, _context);

    [Fact] void should_leave_the_future_in_storage() => _futures.DidNotReceive().ResolveFuture(_futureId);
    [Fact] void should_report_the_failed_partition() => _result.FailedPartitions.ShouldContainOnly(_failure);
    [Fact] void should_not_attempt_a_separate_future_write() => _writingSink.DidNotReceive().ApplyChanges(_futureKey, _futureChangeset, _context.EventSequenceNumber);
}
