// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges;

public class when_a_folded_future_write_succeeds : given.a_pending_future_save
{
    protected override bool FoldIntoMainWrite => true;

    void Establish()
    {
        _context.Changeset.HasChanges.Returns(true);
        _writingSink.ApplyChanges(_context.Key, _context.Changeset, _context.EventSequenceNumber, SinkWriteMode.OnlyWhenAdvancingWatermark).Returns(_ =>
        {
            _operations.Add("write");
            return Task.FromResult<IEnumerable<FailedPartition>>([]);
        });
    }

    async Task Because() => _result = await _step.Perform(_projection, _context);

    [Fact] void should_resolve_the_future_after_the_main_write() => _operations.ShouldEqual(["write", "resolve"]);
    [Fact] void should_remove_the_persisted_future_once() => _futures.Received(1).ResolveFuture(_futureId);
    [Fact] void should_not_attempt_a_separate_future_write() => _writingSink.DidNotReceive().ApplyChanges(_futureKey, _futureChangeset, _context.EventSequenceNumber);
}
