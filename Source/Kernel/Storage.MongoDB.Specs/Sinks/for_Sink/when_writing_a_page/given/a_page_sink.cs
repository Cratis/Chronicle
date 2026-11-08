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

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_writing_a_page.given;

public class a_page_sink(MongoDBFixture fixture) : Specification
{
    protected ISink _sink;
    protected IMongoCollection<BsonDocument> _collection;
    protected int _bulkWrites;
    protected int _reads;
    protected int _directWrites;
    readonly string _databaseName = MongoDBSpecDatabaseNames.New();
    IMongoClient _client;

    protected ISinkPages Pages => (ISinkPages)_sink;

    async Task Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"id":{"type":"string"},"count":{"type":"integer"}}}
            """);
        var readModel = new ReadModelDefinition("model", "model", "Model", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Reducer, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } }, []);
        var formats = new TypeFormats();
        var expando = new ExpandoObjectConverter(formats);
        var converter = new MongoDBConverter(expando, formats, readModel, NullLogger<MongoDBConverter>.Instance);
        var collections = new SinkCollections(readModel, _client.GetDatabase(_databaseName));
        _collection = collections.GetCollection();
        var sinkCollection = Intercept(_collection);
        var sinkCollections = Substitute.For<ISinkCollections>();
        sinkCollections.GetCollection().Returns(sinkCollection);
        _sink = new Sink(readModel, converter, sinkCollections, new ChangesetConverter(readModel, converter, sinkCollections, expando), expando, Substitute.For<IReadModelChangeStreams>());
    }

    protected static Key KeyFor(string value) => new(value, ArrayIndexers.NoIndexers);

    protected static SinkPageWrite Write(string key, int count) =>
        new(KeyFor(key), Changes(count), (ulong)count, SinkWriteMode.Always);

    protected static Changeset<AppendedEvent, ExpandoObject> Changes(int count)
    {
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), null!, new ExpandoObject());
        changeset.SetProperties([PropertyMappers.FromEventValueProvider("count", _ => count)], ArrayIndexers.NoIndexers);
        return changeset;
    }

    protected async Task<BsonDocument?> Document(string key) => await _collection.Find(Builders<BsonDocument>.Filter.Eq("_id", key)).SingleOrDefaultAsync();

    IMongoCollection<BsonDocument> Intercept(IMongoCollection<BsonDocument> collection)
    {
        var intercepted = Substitute.For<IMongoCollection<BsonDocument>>();
        intercepted.CollectionNamespace.Returns(collection.CollectionNamespace);
        intercepted.BulkWriteAsync(Arg.Any<IEnumerable<WriteModel<BsonDocument>>>(), Arg.Any<BulkWriteOptions>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                _bulkWrites++;
                return collection.BulkWriteAsync(info.ArgAt<IEnumerable<WriteModel<BsonDocument>>>(0), info.ArgAt<BulkWriteOptions>(1));
            });
        intercepted.UpdateOneAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                _directWrites++;
                return collection.UpdateOneAsync(info.ArgAt<FilterDefinition<BsonDocument>>(0), info.ArgAt<UpdateDefinition<BsonDocument>>(1), info.ArgAt<UpdateOptions>(2));
            });
        intercepted.FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                _reads++;
                return collection.FindAsync(info.ArgAt<FilterDefinition<BsonDocument>>(0), info.ArgAt<FindOptions<BsonDocument, BsonDocument>>(1));
            });
        return intercepted;
    }

    async Task Destroy() => await _client.DropDatabaseAsync(_databaseName);
}
