// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using MongoDB.Bson;
using MongoDB.Driver;
using NJsonSchema;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_trying_to_find_root_key_by_child_value.given;

public class a_sink_with_test_data : Specification
{
    const string CollectionName = "test_read_model";
    protected Sink _sink;
    protected IMongoDatabase _database;
    protected IMongoCollection<BsonDocument> _collection;
    protected ReadModelDefinition _readModelDefinition;

    void Establish()
    {
        var client = new MongoClient("mongodb://localhost:27017");
        _database = client.GetDatabase($"chronicle_sink_specs_{Guid.NewGuid():N}");
        _collection = _database.GetCollection<BsonDocument>(CollectionName);

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
            ReadModelOwner.Client,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, schema }
            },
            []);

        var mongoDBConverter = Substitute.For<IMongoDBConverter>();
        var collections = new TestCollections(_database, CollectionName);
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

    void Cleanup()
    {
        _database.Client.DropDatabase(_database.DatabaseNamespace.DatabaseName);
    }

    class TestCollections : ISinkCollections
    {
        readonly IMongoDatabase _database;
        readonly string _collectionName;

        public TestCollections(IMongoDatabase database, string collectionName)
        {
            _database = database;
            _collectionName = collectionName;
        }

        public IMongoCollection<BsonDocument> GetCollection() => _database.GetCollection<BsonDocument>(_collectionName);
        public Task BeginReplay(Chronicle.Storage.Sinks.ReplayContext context) => Task.CompletedTask;
        public Task ResumeReplay(Chronicle.Storage.Sinks.ReplayContext context) => Task.CompletedTask;
        public Task EndReplay(Chronicle.Storage.Sinks.ReplayContext context) => Task.CompletedTask;
        public Task PrepareInitialRun() => Task.CompletedTask;
    }
}
