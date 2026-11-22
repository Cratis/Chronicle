// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2.Unit.for_UniqueConstraintsStorageV2;

public class when_classifying_v2_topology
{
    static BsonDocument Primary() => new() { { "setName", "rs0" }, { "isWritablePrimary", true }, { "logicalSessionTimeoutMinutes", 30 }, { "maxWireVersion", 25 } };

    [Fact]
    public void should_accept_a_confirmed_replica_set_primary() => V2Topology.SupportsTransactions(Primary()).ShouldBeTrue();

    [Fact]
    public void should_refuse_standalone_unknown_and_mongos_without_contacting_a_server()
    {
        V2Topology.SupportsTransactions(new BsonDocument()).ShouldBeFalse();
        var standalone = Primary();
        standalone.Remove("setName");
        V2Topology.SupportsTransactions(standalone).ShouldBeFalse();
        var mongos = Primary();
        mongos["msg"] = "isdbgrid";
        V2Topology.SupportsTransactions(mongos).ShouldBeFalse();
    }

    [Theory]
    [InlineData("isWritablePrimary")]
    [InlineData("logicalSessionTimeoutMinutes")]
    [InlineData("maxWireVersion")]
    public void should_require_each_capability(string capability)
    {
        var reply = Primary();
        reply.Remove(capability);
        V2Topology.SupportsTransactions(reply).ShouldBeFalse();
    }
}
