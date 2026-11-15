// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDatabaseMove;

/// <summary>
/// Only a refusal on authorization grounds leaves the source behind; any other failure while dropping its collections
/// must surface, since trying again may succeed.
/// </summary>
public class when_dropping_collections_fails_for_another_reason : Specification
{
    IMongoDatabase _database;
    Exception _exception;

    void Establish()
    {
        var client = Substitute.For<IMongoClient>();
        client
            .DropDatabaseAsync("source", Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new MongoCommandException(
                new ConnectionId(new ServerId(new ClusterId(), new System.Net.DnsEndPoint("localhost", 27017))),
                "not authorized on source to execute command { dropDatabase: 1 }",
                new BsonDocument("dropDatabase", 1),
                new BsonDocument { { "ok", 0 }, { "code", 13 } })));

        var cursor = Substitute.For<IAsyncCursor<string>>();
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        cursor.Current.Returns(["events"]);

        _database = Substitute.For<IMongoDatabase>();
        _database.Client.Returns(client);
        _database.DatabaseNamespace.Returns(new DatabaseNamespace("source"));
        _database.ListCollectionNamesAsync(Arg.Any<ListCollectionNamesOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(cursor));
        _database.DropCollectionAsync("events", Arg.Any<CancellationToken>()).Returns(Task.FromException(new TimeoutException()));
    }

    async Task Because() => _exception = await Catch.Exception(() => MongoDatabaseMove.Drop(_database));

    [Fact] void should_surface_the_failure() => _exception.ShouldBeOfExactType<TimeoutException>();
}
