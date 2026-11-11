// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges;

public class when_many_futures_include_folded_and_separate_writes : given.a_pending_future_save
{
    readonly ProjectionFutureId _second = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    readonly ProjectionFutureId _third = new(Guid.Parse("33333333-3333-3333-3333-333333333333"));
    readonly Key _otherKey = new("other-key", ArrayIndexers.NoIndexers);

    protected override bool FoldIntoMainWrite => true;

    void Establish()
    {
        _context.Changeset.HasChanges.Returns(true);
        _context.AddPendingFutureSave(_otherKey, _futureChangeset, _second);
        _context.AddPendingFutureSave(_context.Key, _futureChangeset, _third);
        _writingSink.ApplyChangesImmediately(_context.Key, _context.Changeset, _context.EventSequenceNumber, SinkWriteMode.OnlyWhenAdvancingWatermark).Returns(_ =>
        {
            _operations.Add("main-write");
            return Task.FromResult<IEnumerable<FailedPartition>>([]);
        });
        _writingSink.ApplyChangesImmediately(_otherKey, _futureChangeset, _context.EventSequenceNumber, SinkWriteMode.Always).Returns(_ =>
        {
            _operations.Add("separate-write");
            return Task.FromResult<IEnumerable<FailedPartition>>([]);
        });
        _futures.ResolveFuture(_second).Returns(_ =>
        {
            _operations.Add("resolve-second");
            return Task.CompletedTask;
        });
        _futures.ResolveFuture(_third).Returns(_ =>
        {
            _operations.Add("resolve-third");
            return Task.CompletedTask;
        });
    }

    async Task Because() => _result = await _step.Perform(_projection, _context);

    [Fact] void should_resolve_each_future_only_after_its_successful_write() => _operations.ShouldEqual(["main-write", "resolve", "resolve-third", "separate-write", "resolve-second"]);
    [Fact] void should_resolve_each_future_once()
    {
        _futures.Received(1).ResolveFuture(_futureId);
        _futures.Received(1).ResolveFuture(_second);
        _futures.Received(1).ResolveFuture(_third);
    }
    [Fact] void should_not_report_failures() => _result.FailedPartitions.ShouldBeEmpty();
}
