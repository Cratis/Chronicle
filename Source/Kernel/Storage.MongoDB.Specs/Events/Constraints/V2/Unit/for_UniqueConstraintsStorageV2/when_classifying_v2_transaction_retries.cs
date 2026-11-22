// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2.Unit.for_UniqueConstraintsStorageV2;

public class when_classifying_v2_transaction_retries
{
    [Theory]
    [InlineData(112)]
    [InlineData(244)]
    [InlineData(246)]
    [InlineData(251)]
    [InlineData(12586)]
    public void should_retry_definite_aborts_including_post_schema_snapshot_errors(int code)
    {
        var connection = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));
        var error = new MongoCommandException(connection, "aborted", new BsonDocument { { "code", code }, { "errmsg", "aborted" } });
        V2Transactions.CanRetry(error).ShouldBeTrue();
    }

    [Fact]
    public void should_never_replay_an_unknown_commit_or_an_unrelated_driver_error()
    {
        var unknown = new MongoException("unknown");
        unknown.AddErrorLabel("UnknownTransactionCommitResult");
        unknown.AddErrorLabel("TransientTransactionError");
        V2Transactions.CanRetry(unknown).ShouldBeFalse();
        V2Transactions.CanRetry(new MongoException("unrelated")).ShouldBeFalse();
    }
}
