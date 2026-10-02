// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ensuring_indexes;

public class and_creating_an_index_fails : given.a_sink_with_indexes
{
    MongoCommandException _failure;
    Exception _error;

    void Establish()
    {
        _failure = new MongoCommandException(
            new ConnectionId(new ServerId(new ClusterId(), new System.Net.DnsEndPoint("localhost", 27017))),
            "Index creation failed",
            new BsonDocument("createIndexes", "Something"),
            new BsonDocument { { "ok", 0 }, { "code", 13 }, { "errmsg", "Unauthorized" } });
        _indexManager.CreateOneAsync(
            Arg.Any<CreateIndexModel<BsonDocument>>(),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromException<string>(_failure));
    }

    async Task Because() => _error = await Catch.Exception(_sink.EnsureIndexes);

    [Fact] void should_propagate_the_failure() => _error.ShouldEqual(_failure);
}
