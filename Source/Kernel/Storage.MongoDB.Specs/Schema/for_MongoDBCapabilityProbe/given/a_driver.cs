// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBCapabilityProbe.given;

public class a_driver : Specification
{
    protected IDatabase _database;
    protected IMongoDatabase _mongo;
    protected IMongoCollection<BsonDocument> _collection;
    protected IMongoIndexManager<BsonDocument> _indexes;
    protected IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> _changes;
    protected IAsyncCursor<BsonDocument> _documents;
    protected IAsyncCursor<string> _collections;
    protected ChronicleOptions _options;
    protected ILogger<MongoDBCapabilityProbe> _logger;
    protected MongoDBCapabilityProbe _probe;
    protected MongoDBCapabilityReport _report;

    void Establish()
    {
        _database = Substitute.For<IDatabase>();
        _mongo = Substitute.For<IMongoDatabase>();
        _collection = Substitute.For<IMongoCollection<BsonDocument>>();
        _indexes = Substitute.For<IMongoIndexManager<BsonDocument>>();
        _changes = Substitute.For<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>>();
        _documents = Substitute.For<IAsyncCursor<BsonDocument>>();
        _collections = Substitute.For<IAsyncCursor<string>>();
        _logger = Substitute.For<ILogger<MongoDBCapabilityProbe>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _options = new() { Storage = new() { SchemaMode = SchemaMode.Migrate } };
        _database.GetCollection<BsonDocument>(MongoDBCapabilityProbe.ProbeCollectionName).Returns(_collection);
        _collection.Database.Returns(_mongo);
        _collection.Indexes.Returns(_indexes);
        _mongo.WatchAsync(
            Arg.Any<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
            Arg.Any<ChangeStreamOptions>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult(_changes));
        _changes.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));
        _collection.FindAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<FindOptions<BsonDocument, BsonDocument>>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult(_documents));
        _documents.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));
        _mongo.ListCollectionNamesAsync(Arg.Any<ListCollectionNamesOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(_collections));
        _collections.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));
        _mongo.CreateCollectionAsync(MongoDBCapabilityProbe.ProbeCollectionName, Arg.Any<CreateCollectionOptions>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _mongo.DropCollectionAsync(MongoDBCapabilityProbe.ProbeCollectionName, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _indexes.CreateOneAsync(Arg.Any<CreateIndexModel<BsonDocument>>(), Arg.Any<CreateOneIndexOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult("capability_collation"));
        _probe = new(_database, Options.Create(_options), _logger);
    }
}
