// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.MongoDB.Integration.Generations.Fencing.for_FencedMongoSink.when_clearing_applied_event_markers_for_a_partition;

[Collection(MongoDBCollection.Name)]
public class and_the_partition_is_replayed(ChronicleInProcessFixture fixture) : FencedSourceKeyedJoinSink(fixture)
{
    int _afterTheFirst;
    BsonDocument _root;

    async Task Because()
    {
        await Apply(EventOf("joined", JoinedStream, 20UL), JoinOf(3));
        await Clear(JoinedStream);
        await Apply(EventOf("joined", JoinedStream, 10UL), JoinOf(2));
        _afterTheFirst = (await Read("root"))["count"].ToInt32();
        await Apply(EventOf("joined", JoinedStream, 20UL), JoinOf(3));
        _root = await Read("root");
    }

    [Fact] void should_rewrite_the_older_event_first() => _afterTheFirst.ShouldEqual(2);
    [Fact] void should_end_with_the_newer_event() => _root["count"].ToInt32().ShouldEqual(3);
}
