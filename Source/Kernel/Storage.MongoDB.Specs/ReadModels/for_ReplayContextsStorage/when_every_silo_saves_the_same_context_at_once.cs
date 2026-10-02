// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Driver;
using MongoReplayContext = Cratis.Chronicle.Storage.MongoDB.ReadModels.ReplayContext;

namespace Cratis.Chronicle.Storage.MongoDB.ReadModels.for_ReplayContextsStorage;

/// <summary>
/// Every silo establishes the replay context when a replay begins. Concurrent upserts of one document race
/// to insert it, and the losers used to fail on the key - failing that silo's start of the replay (#4296).
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/>.</param>
[Collection(MongoDBCollection.Name)]
public class when_every_silo_saves_the_same_context_at_once(MongoDBFixture fixture) : IAsyncLifetime
{
    readonly string _databaseName = MongoDBSpecDatabaseNames.New();
    IMongoClient _client = default!;
    Exception? _error;
    long _stored;

    [Fact] public void should_not_fail() => _error.ShouldBeNull();
    [Fact] public void should_hold_one_context() => _stored.ShouldEqual(1);

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _client = new MongoClient(fixture.ConnectionString);
        var database = _client.GetDatabase(_databaseName);
        var namespaceDatabase = Substitute.For<IEventStoreNamespaceDatabase>();
        namespaceDatabase.GetCollection<MongoReplayContext>(Arg.Any<string?>())
            .Returns(call => database.GetCollection<MongoReplayContext>(call.Arg<string?>()));
        var storage = new ReplayContextsStorage(namespaceDatabase);

        var type = new ReadModelType("thing", ReadModelGeneration.First);
        try
        {
            for (var round = 0; round < 5; round++)
            {
                await storage.Remove(type.Identifier);
                await Task.WhenAll(Enumerable.Range(0, 8).Select(silo =>
                    storage.Save(new Chronicle.Storage.ReadModels.ReplayContext(type, "things", $"things-{silo}", DateTimeOffset.UtcNow))));
            }
        }
        catch (Exception exception)
        {
            _error = exception;
        }

        _stored = await database.GetCollection<MongoReplayContext>(WellKnownCollectionNames.ReplayContexts).CountDocumentsAsync(FilterDefinition<MongoReplayContext>.Empty);
    }

    /// <inheritdoc/>
    public async Task DisposeAsync() => await _client.DropDatabaseAsync(_databaseName);
}
