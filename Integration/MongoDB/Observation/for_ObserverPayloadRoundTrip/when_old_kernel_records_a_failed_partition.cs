// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_ObserverPayloadRoundTrip;

/// <summary>
/// Verifies that an actual released-kernel reactor failure persists only the failing partition.
/// </summary>
public class when_old_kernel_records_a_failed_partition : Cratis.Specifications.Specification
{
    ReleasedKernelFixture _fixture;
    BsonDocument _failedPartition;
    bool _successfulPartitionFailed;
    string _partition;
    string _observerId;

    async Task Establish()
    {
        _fixture = new();
        await _fixture.Start();
    }

    async Task Because()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var result = await ReleasedFailedPartition.Produce(_fixture, deadline.Token);
        _partition = result.Partition;
        _observerId = result.ObserverId;
        var collection = _fixture.Client.GetDatabase(result.Database).GetCollection<BsonDocument>(WellKnownCollectionNames.FailedPartitions);
        _failedPartition = await collection.Find(
            Builders<BsonDocument>.Filter.Eq("observerId", result.ObserverId) &
            Builders<BsonDocument>.Filter.Eq("partition.value", result.Partition)).SingleAsync(deadline.Token);
        _successfulPartitionFailed = await collection.Find(
            Builders<BsonDocument>.Filter.Eq("observerId", result.ObserverId) &
            Builders<BsonDocument>.Filter.Eq("partition.value", result.SucceedingPartition)).AnyAsync(deadline.Token);
    }

    [Fact] void should_persist_the_failed_partition() => _failedPartition["partition"]["value"].AsString.ShouldEqual(_partition);
    [Fact] void should_persist_the_reactor_identity() => _failedPartition["observerId"].AsString.ShouldEqual(_observerId);
    [Fact] void should_record_a_failure_attempt() => _failedPartition["attempts"].AsBsonArray.ShouldNotBeEmpty();
    [Fact] void should_not_fail_the_successfully_handled_partition() => _successfulPartitionFailed.ShouldBeFalse();

    async Task Cleanup()
    {
        if (_fixture is not null) await _fixture.DisposeAsync();
    }
}
