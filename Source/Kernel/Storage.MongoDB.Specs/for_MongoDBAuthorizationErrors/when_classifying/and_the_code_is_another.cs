// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBAuthorizationErrors.when_classifying;

public class and_the_code_is_another : Specification
{
    bool _result;
    string? _command;

    void Because() => _result = MongoDBAuthorizationErrors.IsUnauthorized(
        new MongoCommandException(
            new ConnectionId(new ServerId(new ClusterId(), new System.Net.DnsEndPoint("localhost", 27017))),
            "Bad value",
            new BsonDocument("listIndexes", "EventTypeStatistics"),
            new BsonDocument { { "ok", 0 }, { "code", 2 } }),
        out _command);

    [Fact] void should_not_recognize_the_failure() => _result.ShouldBeFalse();
    [Fact] void should_not_return_a_command_name() => _command.ShouldBeNull();
}
