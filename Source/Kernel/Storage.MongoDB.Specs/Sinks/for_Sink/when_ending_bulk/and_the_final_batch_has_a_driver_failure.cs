// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_bulk;

public class and_the_final_batch_has_a_driver_failure : given.a_sink_with_gated_bulk_writes
{
    readonly CancellationTokenSource _deadline = new();
    readonly TaskCompletionSource _driverStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<BulkWriteResult<BsonDocument>> _driver = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IEnumerable<FailedPartition> _failures = [];
    Exception _error;

    async Task Establish()
    {
        _collection.BulkWriteAsync(Arg.Any<IEnumerable<WriteModel<BsonDocument>>>(), Arg.Any<BulkWriteOptions>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _driverStarted.SetResult();
            return _driver.Task;
        });
        await _sink.BeginBulk();
        await Apply(_firstKey, 1, 12);
        await Apply(_firstKey, 2, 13);
        await Apply(_secondKey, 1, 14);
    }

    async Task Because()
    {
        var stop = _sink.EndBulk(_deadline.Token);
        await _driverStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _driver.SetException(new MongoException("The driver lost its connection"));
        _error = await Catch.Exception(async () => _failures = await stop.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    void Destroy()
    {
        _driver.TrySetCanceled();
        _deadline.Dispose();
        _sink.Dispose();
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_return_before_the_deadline() => _deadline.IsCancellationRequested.ShouldBeFalse();
    [Fact] void should_report_all_affected_partitions() => _failures.Select(failure => failure.EventSourceId).ShouldContainOnly(_firstKey, _secondKey);
    [Fact] void should_keep_the_earliest_unconfirmed_sequence_number() => _failures.Single(failure => failure.EventSourceId == _firstKey).EventSequenceNumber.Value.ShouldEqual(12UL);
    [Fact] void should_describe_each_outcome_as_unknown() => _failures.ShouldEachConformTo(failure => failure.Reason.Contains("outcome unknown", StringComparison.Ordinal));
}
