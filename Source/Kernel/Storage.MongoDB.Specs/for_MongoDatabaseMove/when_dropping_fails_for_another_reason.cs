// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDatabaseMove;

/// <summary>
/// Only a refusal on authorization grounds is worked around; any other failure must surface rather than be
/// answered by dropping collections.
/// </summary>
public class when_dropping_fails_for_another_reason : Specification
{
    IMongoDatabase _database;
    Exception _exception;

    void Establish()
    {
        var client = Substitute.For<IMongoClient>();
        client.DropDatabaseAsync("source", Arg.Any<CancellationToken>()).Returns(Task.FromException(new TimeoutException()));
        _database = Substitute.For<IMongoDatabase>();
        _database.Client.Returns(client);
        _database.DatabaseNamespace.Returns(new DatabaseNamespace("source"));
    }

    async Task Because() => _exception = await Catch.Exception(() => MongoDatabaseMove.Drop(_database));

    [Fact] void should_surface_the_failure() => _exception.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_not_drop_any_collection() => _database.DidNotReceive().DropCollectionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
}
