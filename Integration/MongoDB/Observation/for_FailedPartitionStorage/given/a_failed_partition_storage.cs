// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Observation;
using MongoDB.Driver;
using NSubstitute;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_FailedPartitionStorage.given;

public class a_failed_partition_storage(ChronicleInProcessFixture fixture) : Integration.given.a_mongo_client(fixture)
{
    protected FailedPartitionStorage _storage = default!;
    protected IEventStoreNamespaceDatabase _database = default!;
    protected IMongoDatabase _mongoDatabase = default!;
    protected string _databaseName = default!;

    async Task Establish()
    {
        // MongoDB caps a database name at 63 characters and the suffix alone is 32, so the prefix has to stay short.
        _databaseName = $"failed_partition_specs_{Guid.NewGuid():N}";
        _mongoDatabase = _client.GetDatabase(_databaseName);

        _database = Substitute.For<IEventStoreNamespaceDatabase>();
        _database
            .GetCollection<FailedPartition>(WellKnownCollectionNames.FailedPartitions)
            .Returns(_mongoDatabase.GetCollection<FailedPartition>(WellKnownCollectionNames.FailedPartitions));

        _storage = new FailedPartitionStorage(_database);
        await Task.CompletedTask;
    }

    async Task Cleanup() => await _mongoDatabase.Client.DropDatabaseAsync(_databaseName);

    protected static FailedPartition CreateFailedPartition(ObserverId observerId, Key partition) =>
        new()
        {
            Id = FailedPartitionId.New(),
            ObserverId = observerId,
            Partition = partition
        };
}
