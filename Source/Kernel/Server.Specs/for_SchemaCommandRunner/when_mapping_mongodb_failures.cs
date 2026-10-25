// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.MongoDB.Schema;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Server.for_SchemaCommandRunner;

public class when_mapping_mongodb_failures : Specification
{
    int _authorization;
    int _configuration;
    int _missing;

    void Because()
    {
        var connection = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));
        _authorization = SchemaCommandRunner.ExitCode(new MongoCommandException(connection, "not authorized", new BsonDocument("code", 13)), SchemaAction.VerifyAndExit);
        _configuration = SchemaCommandRunner.ExitCode(new InvalidOperationException("invalid configuration"), SchemaAction.MigrateAndExit);
        _missing = SchemaCommandRunner.ExitCode(new MongoDBIndexesNotProvisioned(new([new("namespace:orders/default", "orders_default", ["event-log/subject_index"], [])])), SchemaAction.VerifyAndExit);
    }

    [Fact] void should_map_authorization_failure_to_one() => _authorization.ShouldEqual(1);
    [Fact] void should_map_configuration_failure_to_four() => _configuration.ShouldEqual(4);
    [Fact] void should_map_missing_indexes_to_two() => _missing.ShouldEqual(2);
}
