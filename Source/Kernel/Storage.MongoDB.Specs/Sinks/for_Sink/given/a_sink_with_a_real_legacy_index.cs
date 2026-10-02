// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.given;

public abstract class a_sink_with_a_real_legacy_index(MongoDBFixture fixture) : Indexing.given.a_real_namespace_database(fixture)
{
    protected const string CollectionName = "legacyReadModel";
    protected Sink _sink;
    IMongoCollection<BsonDocument> _collection;

    void Establish()
    {
        _collection = _rawDatabase.GetCollection<BsonDocument>(CollectionName);
        var collections = Substitute.For<ISinkCollections>();
        collections.GetCollection().Returns(_collection);
        var readModel = new ReadModelDefinition(
            "legacyReadModel",
            CollectionName,
            "Legacy read model",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, new JsonSchema() }
            },
            [new IndexDefinition("p")]);
        _sink = new Sink(
            readModel,
            Substitute.For<IMongoDBConverter>(),
            collections,
            Substitute.For<IChangesetConverter>(),
            Substitute.For<IExpandoObjectConverter>(),
            Substitute.For<IReadModelChangeStreams>());
    }

    protected async Task CreateLegacyIndex(Collation? collation = null)
    {
        await _rawDatabase.CreateCollectionAsync(CollectionName, new CreateCollectionOptions { Collation = collation });
        await _collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("p"),
            new CreateIndexOptions { Name = "p_1" }));
    }
}
