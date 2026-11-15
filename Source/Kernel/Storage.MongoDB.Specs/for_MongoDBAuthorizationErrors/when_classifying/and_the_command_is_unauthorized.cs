// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBAuthorizationErrors.when_classifying;

public class and_the_command_is_unauthorized : Specification
{
    bool _result;
    string? _command;

    void Because() => _result = MongoDBAuthorizationErrors.IsUnauthorized(
        new MongoCommandException(
            new ConnectionId(new ServerId(new ClusterId(), new System.Net.DnsEndPoint("localhost", 27017))),
            "Unauthorized",
            new BsonDocument("listIndexes", "EventTypeStatistics"),
            new BsonDocument { { "ok", 0 }, { "code", 13 } }),
        out _command);

    [Fact] void should_recognize_the_failure() => _result.ShouldBeTrue();
    [Fact] void should_return_only_the_command_name() => _command.ShouldEqual("listIndexes");
}
