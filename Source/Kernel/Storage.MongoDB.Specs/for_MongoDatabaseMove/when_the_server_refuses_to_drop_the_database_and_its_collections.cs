// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDatabaseMove;

/// <summary>
/// A user that may copy but neither drop the database nor its collections has already moved the content, so the
/// refusal must not fail the move: the source is reported as kept for someone with the rights to remove it.
/// </summary>
public class when_the_server_refuses_to_drop_the_database_and_its_collections : Specification
{
    IMongoDatabase _database;
    Exception _exception;
    bool _dropped;

    void Establish()
    {
        var client = Substitute.For<IMongoClient>();
        client
            .DropDatabaseAsync("source", Arg.Any<CancellationToken>())
            .Returns(Task.FromException(Unauthorized("dropDatabase")));

        var cursor = Substitute.For<IAsyncCursor<string>>();
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        cursor.Current.Returns(["events"]);

        _database = Substitute.For<IMongoDatabase>();
        _database.Client.Returns(client);
        _database.DatabaseNamespace.Returns(new DatabaseNamespace("source"));
        _database.ListCollectionNamesAsync(Arg.Any<ListCollectionNamesOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(cursor));
        _database.DropCollectionAsync("events", Arg.Any<CancellationToken>()).Returns(Task.FromException(Unauthorized("drop")));
    }

    async Task Because() => _exception = await Catch.Exception(async () => _dropped = await MongoDatabaseMove.Drop(_database));

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_report_the_database_as_kept() => _dropped.ShouldBeFalse();

    static MongoCommandException Unauthorized(string command) => new(
        new ConnectionId(new ServerId(new ClusterId(), new System.Net.DnsEndPoint("localhost", 27017))),
        $"not authorized on source to execute command {{ {command}: 1 }}",
        new BsonDocument(command, 1),
        new BsonDocument { { "ok", 0 }, { "code", 13 } });
}
