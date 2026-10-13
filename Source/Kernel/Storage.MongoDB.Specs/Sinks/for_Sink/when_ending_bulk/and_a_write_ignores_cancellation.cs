// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_bulk;

public class and_a_write_ignores_cancellation : given.a_sink_with_gated_bulk_writes
{
    readonly CancellationTokenSource _deadline = new();
    readonly TaskCompletionSource _driverStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _driverCancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<BulkWriteResult<BsonDocument>> _driver = new(TaskCreationOptions.RunContinuationsAsynchronously);
    CancellationToken _driverToken;
    IEnumerable<FailedPartition> _failures;

    async Task Establish()
    {
        _collection.BulkWriteAsync(Arg.Any<IEnumerable<WriteModel<BsonDocument>>>(), Arg.Any<BulkWriteOptions>(), Arg.Any<CancellationToken>()).Returns(info =>
        {
            _driverToken = info.ArgAt<CancellationToken>(2);
            _driverToken.Register(() => _driverCancelled.TrySetResult());
            _driverStarted.SetResult();
            return _driver.Task;
        });
        await _sink.BeginBulk();
        await Apply(_firstKey, 1, 12);
    }

    async Task Because()
    {
        var stop = _sink.EndBulk(_deadline.Token);
        await _driverStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _deadline.CancelAsync();
        _failures = await stop.WaitAsync(TimeSpan.FromSeconds(5));
        await _driverCancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    void Destroy()
    {
        _driver.TrySetCanceled();
        _deadline.Dispose();
        _sink.Dispose();
    }

    [Fact] void should_cancel_the_driver_token() => _driverToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_return_without_waiting_for_the_driver() => _driver.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_report_the_unknown_partition() => _failures.Single().EventSourceId.ShouldEqual(_firstKey);
    [Fact] void should_keep_the_earliest_unconfirmed_sequence_number() => _failures.Single().EventSequenceNumber.Value.ShouldEqual(12UL);
    [Fact] void should_describe_the_outcome_as_unknown() => _failures.Single().Reason.ShouldContain("outcome unknown");
}
