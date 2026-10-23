// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_MongoTopology;

public class when_server_is_a_single_node_replica_set : Specification
{
    IMongoDatabase _database;
    Exception? _error;

    void Establish()
    {
        _database = Substitute.For<IMongoDatabase>();
        _database.RunCommandAsync(Arg.Any<Command<BsonDocument>>(), Arg.Any<ReadPreference>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new BsonDocument { { "setName", "rs0" }, { "isWritablePrimary", true } }));
    }

    async Task Because() => _error = await Catch.Exception(() => MongoTopology.RequireTransactions(_database));

    [Fact] void should_allow_fenced_transactions() => _error.ShouldBeNull();
}
