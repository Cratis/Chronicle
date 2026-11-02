// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.MongoDB.Integration.Generations.Fencing.for_FencedMongoSink.when_redelivering_to_a_source_keyed_join_projection;

[Collection(MongoDBCollection.Name)]
public class and_an_older_event_is_redelivered_after_a_newer_join(ChronicleInProcessFixture fixture) : FencedSourceKeyedJoinSink(fixture)
{
    BsonDocument _root;
    BsonDocument _other;

    async Task Because()
    {
        await Apply(EventOf("own", "root", 10UL), Direct(1));
        await Apply(EventOf("joined", JoinedStream, 20UL), JoinOf(3));
        await Apply(EventOf("own", "root", 10UL), Direct(1));
        await Apply(EventOf("joined", JoinedStream, 10UL), JoinOf(2));
        _root = await Read("root");
        _other = await Read("other");
    }

    [Fact] void should_keep_the_newer_joined_value_on_the_root() => _root["count"].ToInt32().ShouldEqual(3);
    [Fact] void should_keep_the_newer_joined_value_on_the_other_row() => _other["count"].ToInt32().ShouldEqual(3);
}
