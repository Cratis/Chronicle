// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges;

public class when_an_unrelated_drained_write_fails : given.a_pending_future_save
{
    FailedPartition _failure;

    async Task Establish()
    {
        await _writingSink.BeginBulk();
        _failure = new FailedPartition(_context.Key, _context.EventSequenceNumber);
        _writingSink.ApplyChangesImmediately(_futureKey, _futureChangeset, _context.EventSequenceNumber, SinkWriteMode.Always).Returns(Task.FromResult<IEnumerable<FailedPartition>>([_failure]));
    }

    async Task Because() => _result = await _step.Perform(_projection, _context);

    [Fact] void should_resolve_the_successful_future() => _futures.Received(1).ResolveFuture(_futureId);
    [Fact] void should_report_the_drained_failure() => _result.FailedPartitions.ShouldContainOnly(_failure);
}
