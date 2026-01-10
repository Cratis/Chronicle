// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;
using NJsonSchema;
using NSubstitute;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_trying_to_find_root_key_by_child_value.given;

public class a_sink_with_test_data(ChronicleInProcessFixture fixture) : Specification(fixture)
{
    protected Sink _sink;
    protected IMongoDatabase _database;
    protected IMongoCollection<BsonDocument> _collection;
    protected ReadModelDefinition _readModelDefinition;

    void Establish()
    {
        var client = new MongoClient($"mongodb://localhost:{XUnit.Integration.ChronicleFixture.MongoDBPort}");
        _database = client.GetDatabase($"chronicle_sink_specs_{Guid.NewGuid():N}");
        const string collectionName = "test_read_model";
        _collection = _database.GetCollection<BsonDocument>(collectionName);

        var schema = new JsonSchema
        {
            Type = JsonObjectType.Object,
            Properties =
            {
                ["_id"] = new JsonSchemaProperty { Type = JsonObjectType.String },
                ["name"] = new JsonSchemaProperty { Type = JsonObjectType.String },
                ["children"] = new JsonSchemaProperty
                {
                    Type = JsonObjectType.Array,
                    Item = new JsonSchema
                    {
                        Type = JsonObjectType.Object,
                        Properties =
                        {
                            ["childId"] = new JsonSchemaProperty { Type = JsonObjectType.String },
                            ["name"] = new JsonSchemaProperty { Type = JsonObjectType.String }
                        }
                    }
                },
                ["configurations"] = new JsonSchemaProperty
                {
                    Type = JsonObjectType.Array,
                    Item = new JsonSchema
                    {
                        Type = JsonObjectType.Object,
                        Properties =
                        {
                            ["configurationId"] = new JsonSchemaProperty { Type = JsonObjectType.String },
                            ["name"] = new JsonSchemaProperty { Type = JsonObjectType.String },
                            ["hubs"] = new JsonSchemaProperty
                            {
                                Type = JsonObjectType.Array,
                                Item = new JsonSchema
                                {
                                    Type = JsonObjectType.Object,
                                    Properties =
                                    {
                                        ["hubId"] = new JsonSchemaProperty { Type = JsonObjectType.String },
                                        ["name"] = new JsonSchemaProperty { Type = JsonObjectType.String }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        _readModelDefinition = new ReadModelDefinition(
            "test-read-model",
            "TestReadModel",
            "TestReadModel",
            ReadModelOwner.Client,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, schema }
            },
            []);

        var mongoDBConverter = Substitute.For<IMongoDBConverter>();
        var collections = new TestCollections(_database, collectionName);
        var changesetConverter = Substitute.For<IChangesetConverter>();
        var expandoObjectConverter = Substitute.For<IExpandoObjectConverter>();
        _sink = new Sink(
            _readModelDefinition,
            mongoDBConverter,
            collections,
            changesetConverter,
            expandoObjectConverter);
    }

    protected void InsertDocument(Guid id, BsonDocument document)
    {
        document["_id"] = id.ToString();
        _collection.InsertOne(document);
    }

    async Task Destroy()
    {
        await _database.Client.DropDatabaseAsync(_database.DatabaseNamespace.DatabaseName);
    }

    class TestCollections(IMongoDatabase database, string collectionName) : ISinkCollections
    {
        public IMongoCollection<BsonDocument> GetCollection() => database.GetCollection<BsonDocument>(collectionName);
        public IMongoCollection<BsonDocument> GetCollection(string collectionName) => database.GetCollection<BsonDocument>(collectionName);
        public Task BeginReplay(Storage.ReadModels.ReplayContext context) => Task.CompletedTask;
        public Task ResumeReplay(Storage.ReadModels.ReplayContext context) => Task.CompletedTask;
        public Task EndReplay(Storage.ReadModels.ReplayContext context) => Task.CompletedTask;
        public Task PrepareInitialRun() => Task.CompletedTask;
    }
}
