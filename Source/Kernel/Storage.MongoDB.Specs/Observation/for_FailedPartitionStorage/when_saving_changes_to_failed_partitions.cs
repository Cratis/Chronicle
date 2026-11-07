// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_FailedPartitionStorage;

/// <summary>
/// Proves that saving the failed partitions of an observer stores what is new or changed, removes what is resolved,
/// and keeps doing so when the same state is saved again, against a real MongoDB.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
[Collection(MongoDBCollection.Name)]
public class when_saving_changes_to_failed_partitions(MongoDBFixture fixture) : Specification
{
    static readonly ObserverId _observerId = "a3a6d6a4-0d0e-4b6a-9d3e-6a8f7a3c9b11";
    static readonly Key _kept = "kept";
    static readonly Key _changed = "changed";
    static readonly Key _resolved = "resolved";

    FailedPartitionStorage _storage = default!;
    FailedPartitions _failedPartitions = default!;
    FailedPartitions _stored = default!;
    FailedPartitions _storedAfterRemovedBehindItsBack = default!;

    void Establish()
    {
        var database = new MongoClient(fixture.ConnectionString).GetDatabase(MongoDBSpecDatabaseNames.New());
        var namespaceDatabase = Substitute.For<IEventStoreNamespaceDatabase>();
        namespaceDatabase.GetCollection<FailedPartition>(WellKnownCollectionNames.FailedPartitions)
            .Returns(database.GetCollection<FailedPartition>(WellKnownCollectionNames.FailedPartitions));
        _storage = new(namespaceDatabase);

        _failedPartitions = new();
        _failedPartitions.RegisterAttempt(_kept, 1L, ["kept"], string.Empty);
        _failedPartitions.RegisterAttempt(_changed, 2L, ["changed"], string.Empty);
        _failedPartitions.RegisterAttempt(_resolved, 3L, ["resolved"], string.Empty);
        foreach (var partition in _failedPartitions.Partitions)
        {
            partition.ObserverId = _observerId;
        }
    }

    async Task Because()
    {
        await _storage.Save(_observerId, _failedPartitions);

        _failedPartitions.RegisterAttempt(_changed, 2L, ["changed again"], string.Empty);
        _failedPartitions.Remove(_resolved);
        await _storage.Save(_observerId, _failedPartitions);
        await _storage.Save(_observerId, _failedPartitions);

        _stored = await _storage.GetFor(_observerId);

        await _storage.RemoveAllFor(_observerId);
        await _storage.Save(_observerId, _failedPartitions);
        _storedAfterRemovedBehindItsBack = await _storage.GetFor(_observerId);
    }

    [Fact] void should_store_the_partitions_that_are_still_failing() => _stored.Partitions.Select(_ => _.Partition).ShouldContainOnly(_kept, _changed);
    [Fact] void should_store_the_attempts_that_were_added() => _stored.Partitions.Single(_ => _.Partition == _changed).Attempts.Count().ShouldEqual(2);
    [Fact] void should_not_keep_the_resolved_partition() => _stored.IsFailed(_resolved).ShouldBeFalse();
    [Fact] void should_store_them_again_when_they_were_removed_in_the_meantime() => _storedAfterRemovedBehindItsBack.Partitions.Count().ShouldEqual(2);
}
