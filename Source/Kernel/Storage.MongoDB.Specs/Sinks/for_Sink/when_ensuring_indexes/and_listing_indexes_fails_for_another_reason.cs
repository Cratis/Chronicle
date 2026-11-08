// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ensuring_indexes;

public class and_listing_indexes_fails_for_another_reason : given.a_sink_with_indexes
{
    MongoCommandException _failure;
    Exception _error;

    void Establish()
    {
        _failure = new MongoCommandException(
            new ConnectionId(new ServerId(new ClusterId(), new System.Net.DnsEndPoint("localhost", 27017))),
            "Bad value",
            new BsonDocument("listIndexes", "EventTypeStatistics"),
            new BsonDocument { { "ok", 0 }, { "code", 2 } });
        _indexManager.ListAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<IAsyncCursor<BsonDocument>>(_failure));
    }

    async Task Because() => _error = await Catch.Exception(_sink.EnsureIndexes);

    [Fact] void should_propagate_the_failure() => _error.ShouldBeSame(_failure);
    [Fact] void should_not_record_a_failure() => _authorizationDiagnostics.DidNotReceive().Record(Arg.Any<StorageAuthorizationFailure>());
}
