// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Observation;
using MongoDB.Driver;
using NSubstitute;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_ObserverStateStorage.given;

public class an_observer_state_storage(ChronicleInProcessFixture fixture) : Integration.given.a_mongo_client(fixture)
{
    protected ObserverStateStorage _storage = default!;
    protected IEventStoreNamespaceDatabase _database = default!;
    protected IMongoDatabase _mongoDatabase = default!;
    protected string _databaseName = default!;

    async Task Establish()
    {
        // MongoDB caps a database name at 63 characters and the suffix alone is 32, so the prefix has to stay short.
        _databaseName = $"observer_state_specs_{Guid.NewGuid():N}";
        _mongoDatabase = _client.GetDatabase(_databaseName);

        _database = Substitute.For<IEventStoreNamespaceDatabase>();
        _database
            .GetObserverStateCollection()
            .Returns(_mongoDatabase.GetCollection<Storage.MongoDB.Observation.ObserverState>(WellKnownCollectionNames.Observers));
        _database
            .GetCollection<ObserverPartitionCounts>(WellKnownCollectionNames.ObserverHandledCounts)
            .Returns(_mongoDatabase.GetCollection<ObserverPartitionCounts>(WellKnownCollectionNames.ObserverHandledCounts));

        _storage = new ObserverStateStorage(_database);
        await Task.CompletedTask;
    }

    async Task Cleanup() => await _mongoDatabase.Client.DropDatabaseAsync(_databaseName);

    protected static KernelObserverState CreateState(ObserverId observerId) =>
        KernelObserverState.Empty with
        {
            Identifier = observerId,
            RunningState = ObserverRunningState.Disconnected
        };
}
