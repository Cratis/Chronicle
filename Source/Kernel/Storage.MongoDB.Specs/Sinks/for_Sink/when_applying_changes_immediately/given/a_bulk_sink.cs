// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_immediately.given;

public class a_bulk_sink(MongoDBFixture fixture) : Specification
{
    protected ISink _sink;
    protected IMongoCollection<BsonDocument> _collection;
    protected readonly Key _queuedKey = new("queued", ArrayIndexers.NoIndexers);
    protected readonly Key _futureKey = new("future", ArrayIndexers.NoIndexers);
    readonly string _databaseName = MongoDBSpecDatabaseNames.New();
    IMongoClient _client;

    async Task Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"id":{"type":"string"},"count":{"type":"integer"},"info":{"type":"object","properties":{"name":{"type":"string"}}}}}
            """);
        var readModel = new ReadModelDefinition("model", "model", "Model", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } }, []);
        var formats = new TypeFormats();
        var expando = new ExpandoObjectConverter(formats);
        var converter = new MongoDBConverter(expando, formats, readModel, NullLogger<MongoDBConverter>.Instance);
        var collections = new SinkCollections(readModel, _client.GetDatabase(_databaseName));
        _collection = collections.GetCollection();
        var sinkCollection = InterceptCollection(_collection);
        var sinkCollections = Substitute.For<ISinkCollections>();
        sinkCollections.GetCollection().Returns(sinkCollection);
        _sink = new Sink(readModel, converter, sinkCollections, new ChangesetConverter(readModel, converter, sinkCollections, expando), expando, Substitute.For<IReadModelChangeStreams>());
        await _sink.BeginBulk();
    }

    protected virtual IMongoCollection<BsonDocument> InterceptCollection(IMongoCollection<BsonDocument> collection) => collection;

    protected static Changeset<AppendedEvent, ExpandoObject> Changes(int count)
    {
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), null!, new ExpandoObject());
        changeset.SetProperties([PropertyMappers.FromEventValueProvider("count", _ => count)], ArrayIndexers.NoIndexers);
        return changeset;
    }

    protected async Task<BsonDocument?> Document(Key key) => await _collection.Find(Builders<BsonDocument>.Filter.Eq("_id", key.Value.ToString())).SingleOrDefaultAsync();

    protected async Task RejectDuplicateCounts()
    {
        await _collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("count"), new CreateIndexOptions { Unique = true }));
        await _collection.InsertOneAsync(new BsonDocument { { "_id", "existing" }, { "count", 1 } });
    }

    async Task Destroy() => await _client.DropDatabaseAsync(_databaseName);
}
