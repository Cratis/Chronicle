// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Compliance.for_EncryptionKeyStorageContract;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorageContract;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_running_the_contract(ReplicaSetMongoDBFixture fixture) : EncryptionKeyStorageContract
{
    IMongoDatabase _mongo;
    MongoClient _client;
    TaskCompletionSource _staged;
    TaskCompletionSource _resume;
    bool _pausePublication;
    bool _pauseErasure;
    bool _loseResponse;

    protected override IEncryptionKeyStorage CreateStorage()
    {
        _client = new MongoClient(fixture.ConnectionString);
        _mongo = _client.GetDatabase($"key-fence-contract-{Guid.NewGuid():N}");
        var keys = WrapKeys(_mongo.GetCollection<EncryptionKeyForIdentifier>("encryption-keys").WithWriteConcern(WriteConcern.WMajority));
        var fences = WrapFences(_mongo.GetCollection<EncryptionKeyErasureForIdentifier>("encryption-key-erasures")
            .WithWriteConcern(WriteConcern.WMajority).WithReadConcern(ReadConcern.Linearizable).WithReadPreference(ReadPreference.Primary));
        var database = Substitute.For<IDatabase>();
        var eventStore = Substitute.For<IEventStoreDatabase>();
        var namespaceDatabase = Substitute.For<IEventStoreNamespaceDatabase>();
        database.GetEventStoreDatabase(Arg.Any<EventStoreName>()).Returns(eventStore);
        eventStore.GetNamespaceDatabase(Arg.Any<EventStoreNamespaceName>()).Returns(namespaceDatabase);
        namespaceDatabase.GetCollection<EncryptionKeyForIdentifier>("encryption-keys").Returns(keys);
        namespaceDatabase.GetCollection<EncryptionKeyErasureForIdentifier>("encryption-key-erasures").Returns(fences);

        // The request archive and external fences are untyped collections; an auto-substitute would silently drop archive writes.
        namespaceDatabase.GetCollection<BsonDocument>(Arg.Any<string?>()).Returns(call => _mongo.GetCollection<BsonDocument>(call.Arg<string?>()));
        return new EncryptionKeyStorage(database);
    }

    protected override Task DuringPublication(Func<Task> write, Func<Task> competingOperation)
    {
        _pausePublication = true;
        return DuringOperation(write, competingOperation);
    }

    protected override Task DuringErasure(Func<Task> erase, Func<Task> competingOperation)
    {
        _pauseErasure = true;
        return DuringOperation(erase, competingOperation);
    }

    async Task DuringOperation(Func<Task> operation, Func<Task> competingOperation)
    {
        _staged = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = operation();
        try
        {
            await _staged.Task.WaitAsync(TimeSpan.FromSeconds(30));
            await competingOperation();
        }
        finally
        {
            _resume.TrySetResult();
        }

        await pending;
    }

    protected override async Task LoseWriteResponse(Func<Task> write)
    {
        _loseResponse = true;
        await write();
    }

    async Task Destroy() => await _client.DropDatabaseAsync(_mongo.DatabaseNamespace.DatabaseName);

    IMongoCollection<EncryptionKeyForIdentifier> WrapKeys(IMongoCollection<EncryptionKeyForIdentifier> actual)
    {
        var proxy = Substitute.For<IMongoCollection<EncryptionKeyForIdentifier>>();
        proxy.CollectionNamespace.Returns(actual.CollectionNamespace);
        proxy.Indexes.Returns(actual.Indexes);
        proxy.WithWriteConcern(Arg.Any<WriteConcern>()).Returns(proxy);
        proxy.WithReadConcern(Arg.Any<ReadConcern>()).Returns(actual.WithReadConcern(ReadConcern.Linearizable));
        proxy.FindAsync(Arg.Any<FilterDefinition<EncryptionKeyForIdentifier>>(), Arg.Any<FindOptions<EncryptionKeyForIdentifier, EncryptionKeyForIdentifier>>(), Arg.Any<CancellationToken>())
            .Returns(call => actual.FindAsync(call.Arg<FilterDefinition<EncryptionKeyForIdentifier>>(), call.Arg<FindOptions<EncryptionKeyForIdentifier, EncryptionKeyForIdentifier>>(), call.Arg<CancellationToken>()));
        proxy.InsertOneAsync(Arg.Any<EncryptionKeyForIdentifier>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await actual.InsertOneAsync(call.Arg<EncryptionKeyForIdentifier>(), call.Arg<InsertOneOptions>(), call.Arg<CancellationToken>());
                if (_pausePublication)
                {
                    _pausePublication = false;
                    _staged.TrySetResult();
                    await _resume.Task;
                }
            });
        proxy.UpdateOneAsync(Arg.Any<FilterDefinition<EncryptionKeyForIdentifier>>(), Arg.Any<UpdateDefinition<EncryptionKeyForIdentifier>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(call => actual.UpdateOneAsync(call.Arg<FilterDefinition<EncryptionKeyForIdentifier>>(), call.Arg<UpdateDefinition<EncryptionKeyForIdentifier>>(), call.Arg<UpdateOptions>(), call.Arg<CancellationToken>()));
        proxy.DeleteOneAsync(Arg.Any<FilterDefinition<EncryptionKeyForIdentifier>>(), Arg.Any<CancellationToken>())
            .Returns(call => actual.DeleteOneAsync(call.Arg<FilterDefinition<EncryptionKeyForIdentifier>>(), call.Arg<CancellationToken>()));
        proxy.DeleteManyAsync(Arg.Any<FilterDefinition<EncryptionKeyForIdentifier>>(), Arg.Any<CancellationToken>())
            .Returns(call => actual.DeleteManyAsync(call.Arg<FilterDefinition<EncryptionKeyForIdentifier>>(), call.Arg<CancellationToken>()));
        return proxy;
    }

    IMongoCollection<EncryptionKeyErasureForIdentifier> WrapFences(IMongoCollection<EncryptionKeyErasureForIdentifier> actual)
    {
        var proxy = Substitute.For<IMongoCollection<EncryptionKeyErasureForIdentifier>>();
        proxy.WithWriteConcern(Arg.Any<WriteConcern>()).Returns(proxy);
        proxy.WithReadConcern(Arg.Any<ReadConcern>()).Returns(proxy);
        proxy.WithReadPreference(Arg.Any<ReadPreference>()).Returns(proxy);
        proxy.FindAsync(Arg.Any<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<FindOptions<EncryptionKeyErasureForIdentifier, EncryptionKeyErasureForIdentifier>>(), Arg.Any<CancellationToken>())
            .Returns(call => actual.FindAsync(call.Arg<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), call.Arg<FindOptions<EncryptionKeyErasureForIdentifier, EncryptionKeyErasureForIdentifier>>(), call.Arg<CancellationToken>()));
        proxy.UpdateOneAsync(Arg.Any<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<UpdateDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(call => actual.UpdateOneAsync(call.Arg<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), call.Arg<UpdateDefinition<EncryptionKeyErasureForIdentifier>>(), call.Arg<UpdateOptions>(), call.Arg<CancellationToken>()));
        proxy.FindOneAndUpdateAsync(Arg.Any<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<UpdateDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<FindOneAndUpdateOptions<EncryptionKeyErasureForIdentifier>>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                if (_pauseErasure)
                {
                    _pauseErasure = false;
                    _staged.TrySetResult();
                    await _resume.Task;
                }

                var result = await actual.FindOneAndUpdateAsync(call.Arg<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), call.Arg<UpdateDefinition<EncryptionKeyErasureForIdentifier>>(), call.Arg<FindOneAndUpdateOptions<EncryptionKeyErasureForIdentifier>>(), call.Arg<CancellationToken>());
                if (_loseResponse && result is not null)
                {
                    _loseResponse = false;
                    throw new TimeoutException("Applied fence write response was lost");
                }

                return result!;
            });
        return proxy;
    }
}
