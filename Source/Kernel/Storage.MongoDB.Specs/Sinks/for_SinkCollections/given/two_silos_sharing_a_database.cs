// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.ReadModels;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCollections.given;

/// <summary>
/// Two <see cref="SinkCollections"/> for the same read model over one database - what two silos each hold
/// for a read model, since every silo builds its own sink and every silo is told a replay begins and ends.
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/>.</param>
public abstract class two_silos_sharing_a_database(MongoDBFixture fixture) : IAsyncLifetime
{
    protected const string ContainerName = "things";
    protected const string ReplayName = "replay-things";
    protected const string PromotingName = "replay-things-promoting";

    readonly string _databaseName = MongoDBSpecDatabaseNames.New();
    IMongoClient _client = default!;

    protected IMongoDatabase Database { get; private set; } = default!;
    protected SinkCollections FirstSilo { get; private set; } = default!;
    protected SinkCollections SecondSilo { get; private set; } = default!;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _client = new MongoClient(fixture.ConnectionString);
        Database = _client.GetDatabase(_databaseName);
        var readModel = new ReadModelDefinition(
            "thing",
            ContainerName,
            "Thing",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>(),
            []);
        FirstSilo = new SinkCollections(readModel, Database);
        SecondSilo = new SinkCollections(readModel, Database);

        await Database.GetCollection<BsonDocument>(ContainerName).InsertOneAsync(new BsonDocument { { "_id", "thing" }, { "version", "before" } });
        await Establish();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync() => await _client.DropDatabaseAsync(_databaseName);

    /// <summary>
    /// Brings the database to the state the specification starts from.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected abstract Task Establish();

    protected static ReplayContext ContextRevertingTo(string revertName) =>
        new(new ReadModelType("thing", ReadModelGeneration.First), ContainerName, revertName, DateTimeOffset.UtcNow);

    protected async Task<IReadOnlyList<string>> CollectionNames() =>
        await (await Database.ListCollectionNamesAsync()).ToListAsync();

    protected async Task<string?> VersionIn(string collectionName)
    {
        var document = await Database.GetCollection<BsonDocument>(collectionName).Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();
        return document?["version"].AsString;
    }

    protected static Task Write(SinkCollections silo, string version) =>
        silo.GetCollection().ReplaceOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", "thing"),
            new BsonDocument { { "_id", "thing" }, { "version", version } },
            new ReplaceOptions { IsUpsert = true });
}
