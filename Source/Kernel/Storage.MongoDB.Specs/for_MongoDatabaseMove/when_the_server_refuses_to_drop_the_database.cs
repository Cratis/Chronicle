// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDatabaseMove;

/// <summary>
/// The application user Atlas hands out holds readWriteAnyDatabase, which allows dropping collections but not
/// the database, so a refused dropDatabase must not leave the move failing: the collections are dropped instead.
/// </summary>
public class when_the_server_refuses_to_drop_the_database : Specification
{
    IMongoClient _client;
    IMongoDatabase _database;

    void Establish()
    {
        _client = Substitute.For<IMongoClient>();
        _client
            .DropDatabaseAsync("source", Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new MongoCommandException(
                new ConnectionId(new ServerId(new ClusterId(), new System.Net.DnsEndPoint("localhost", 27017))),
                "not authorized on source to execute command { dropDatabase: 1 }",
                new BsonDocument("dropDatabase", 1),
                new BsonDocument { { "ok", 0 }, { "code", 13 } })));

        var cursor = Substitute.For<IAsyncCursor<string>>();
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        cursor.Current.Returns(["events", "system.views", "jobs"]);

        _database = Substitute.For<IMongoDatabase>();
        _database.Client.Returns(_client);
        _database.DatabaseNamespace.Returns(new DatabaseNamespace("source"));
        _database.ListCollectionNamesAsync(Arg.Any<ListCollectionNamesOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(cursor));
    }

    bool _dropped;

    async Task Because() => _dropped = await MongoDatabaseMove.Drop(_database);

    [Fact] void should_report_the_database_as_dropped() => _dropped.ShouldBeTrue();
    [Fact] void should_drop_the_events_collection() => _database.Received(1).DropCollectionAsync("events", Arg.Any<CancellationToken>());
    [Fact] void should_drop_the_jobs_collection() => _database.Received(1).DropCollectionAsync("jobs", Arg.Any<CancellationToken>());
    [Fact] void should_leave_the_system_collections_alone() => _database.DidNotReceive().DropCollectionAsync("system.views", Arg.Any<CancellationToken>());
}
