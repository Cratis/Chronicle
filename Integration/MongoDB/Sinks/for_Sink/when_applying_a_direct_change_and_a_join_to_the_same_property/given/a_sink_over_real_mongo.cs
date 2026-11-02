// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_applying_a_direct_change_and_a_join_to_the_same_property.given;

public class a_sink_over_real_mongo(ChronicleInProcessFixture fixture) : Integration.given.a_mongo_client(fixture)
{
    protected const string Watermark = "__lastHandledEventSequenceNumber";
    protected static readonly Key RootKey = new("root", ArrayIndexers.NoIndexers);

    protected IMongoDatabase _database;
    protected IMongoCollection<BsonDocument> _documents;
    protected ReadModelDefinition _readModel;

    async Task Establish()
    {
        _database = _client.GetDatabase($"chronicle_join_order_{Guid.NewGuid():N}");
        await _database.CreateCollectionAsync("models");
        _documents = _database.GetCollection<BsonDocument>("models");
        await _documents.InsertManyAsync(
        [
            new BsonDocument { ["_id"] = "root", ["joinId"] = "root", ["count"] = 0, ["x"] = 0, ["retained"] = "untouched" },
            new BsonDocument { ["_id"] = "other", ["joinId"] = "root", ["count"] = 0 }
        ]);
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"joinId":{"type":"string"},"count":{"type":"integer"},"x":{"type":"integer"},"retained":{"type":"string"}}}
            """);
        _readModel = new ReadModelDefinition("model", "models", "Model", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } }, []);
    }

    protected static PropertiesChanged<ExpandoObject> Direct(params (string Property, int Value)[] values) =>
        new(new ExpandoObject(), values.Select(_ => new PropertyDifference(new PropertyPath(_.Property), null, _.Value)).ToArray());

    protected static Joined Join(bool hasKeyedFrom, params (string Property, int Value)[] values) =>
        new(new ExpandoObject(), "root", new PropertyPath("joinId"), ArrayIndexers.NoIndexers, [Direct(values)])
        {
            HasKeyedFrom = hasKeyedFrom
        };

    protected static IChangeset<AppendedEvent, ExpandoObject> ChangesetOf(params Change[] changes)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns(changes);
        changeset.HasJoined().Returns(changes.OfType<Joined>().Any());
        return changeset;
    }

    protected Sink CreateSink(ISinkWrites writes)
    {
        var formats = new TypeFormats();
        var expando = new ExpandoObjectConverter(formats);
        var collections = new SinkCollections(_readModel, _database, null, writes);
        var converter = new MongoDBConverter(expando, formats, _readModel, NullLogger<MongoDBConverter>.Instance);
        var changes = new ChangesetConverter(_readModel, converter, collections, expando, NullLogger<ChangesetConverter>.Instance, writes);
        return new Sink(_readModel, converter, collections, changes, expando, new ReadModelChangeStreams(NullLogger<ReadModelChangeStreams>.Instance), NullLogger<Sink>.Instance, writes);
    }

    protected Task<BsonDocument> Read(string id) => _documents.Find(Builders<BsonDocument>.Filter.Eq("_id", id)).SingleAsync();

    protected static long WatermarkOf(BsonDocument document) => document[Watermark].ToInt64();

    async Task Destroy() => await _client.DropDatabaseAsync(_database.DatabaseNamespace.DatabaseName);
}
