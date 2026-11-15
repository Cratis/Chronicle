// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDatabaseMove;

/// <summary>
/// The source of a move is the only copy of whatever did not make it across, so it must survive a copy that comes up short.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class when_the_copy_cannot_be_confirmed(MongoDBFixture fixture) : Specification
{
    readonly string _name = $"move_{Guid.NewGuid().ToString("N")[..8]}";
    MongoClient _client;
    IMongoDatabase _source;
    IMongoDatabase _target;
    DatabaseMoveOutcome _outcome;

    async Task Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        _source = _client.GetDatabase(_name);
        await _source.GetCollection<BsonDocument>("events").InsertManyAsync([new BsonDocument("_id", 0), new BsonDocument("_id", 1)]);

        // A target that accepts every write and then reports fewer documents than were written.
        var collection = Substitute.For<IMongoCollection<BsonDocument>>();
        collection.CountDocumentsAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(1L));
        _target = Substitute.For<IMongoDatabase>();
        _target.DatabaseNamespace.Returns(new DatabaseNamespace($"{_name}_target"));
        _target.GetCollection<BsonDocument>("events", Arg.Any<MongoCollectionSettings>()).Returns(collection);
    }

    async Task Because() => _outcome = await MongoDatabaseMove.Move(_source, _target, NullLogger.Instance);

    [Fact] void should_report_that_the_source_was_kept() => _outcome.ShouldEqual(DatabaseMoveOutcome.Kept);
    [Fact] async Task should_keep_the_source_database() => (await (await _client.ListDatabaseNamesAsync()).ToListAsync()).ShouldContain(_name);
    [Fact] async Task should_keep_every_document_of_the_source() => (await _source.GetCollection<BsonDocument>("events").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).ShouldEqual(2L);

    async Task Destroy() => await _client.DropDatabaseAsync(_name);
}
