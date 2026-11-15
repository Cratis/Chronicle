// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBTopology;

public class when_classifying_hello_replies : Specification
{
    [Fact] void should_classify_a_plain_server_as_standalone() => MongoDBTopology.IsStandaloneReply(new BsonDocument("isWritablePrimary", true)).ShouldBeTrue();
    [Fact] void should_classify_a_replica_set_member_as_replicated() => MongoDBTopology.IsStandaloneReply(new BsonDocument { { "isWritablePrimary", true }, { "setName", "rs0" } }).ShouldBeFalse();
    [Fact] void should_classify_an_uninitiated_replica_set_member_as_replicated() => MongoDBTopology.IsStandaloneReply(new BsonDocument("isreplicaset", true)).ShouldBeFalse();
    [Fact] void should_classify_a_router_as_replicated() => MongoDBTopology.IsStandaloneReply(new BsonDocument("msg", "isdbgrid")).ShouldBeFalse();
}
