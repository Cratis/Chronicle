// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_MongoTopology;

public class when_the_same_destination_is_checked_again : Specification
{
    IMongoDatabase _first;
    IMongoDatabase _second;

    void Establish()
    {
        var client = Substitute.For<IMongoClient>();
        _first = Substitute.For<IMongoDatabase>();
        _second = Substitute.For<IMongoDatabase>();
        foreach (var database in new[] { _first, _second })
        {
            database.Client.Returns(client);
            database.DatabaseNamespace.Returns(new DatabaseNamespace("models"));
            database.RunCommandAsync(Arg.Any<Command<BsonDocument>>(), Arg.Any<ReadPreference>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new BsonDocument("setName", "rs0")));
        }
    }

    async Task Because()
    {
        await MongoTopology.RequireTransactions(_first);
        await MongoTopology.RequireTransactions(_second);
    }

    [Fact] void should_probe_once_per_client_and_database() => _first.Received(1).RunCommandAsync(Arg.Any<Command<BsonDocument>>(), Arg.Any<ReadPreference>(), Arg.Any<CancellationToken>());
    [Fact] void should_reuse_the_result_across_database_handles() => _second.DidNotReceive().RunCommandAsync(Arg.Any<Command<BsonDocument>>(), Arg.Any<ReadPreference>(), Arg.Any<CancellationToken>());
}
