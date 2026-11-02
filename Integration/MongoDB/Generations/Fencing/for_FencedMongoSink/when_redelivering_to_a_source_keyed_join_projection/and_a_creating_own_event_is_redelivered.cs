// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.MongoDB.Integration.Generations.Fencing.for_FencedMongoSink.when_redelivering_to_a_source_keyed_join_projection;

[Collection(MongoDBCollection.Name)]
public class and_a_creating_own_event_is_redelivered(ChronicleInProcessFixture fixture) : FencedSourceKeyedJoinSink(fixture)
{
    Exception? _error;
    BsonDocument _fresh;

    async Task Because()
    {
        await Apply(EventOf("own", "fresh", 10UL), Direct(1));
        _error = await Catch.Exception(() => Apply(EventOf("own", "fresh", 10UL), Direct(1)));
        _fresh = await Read("fresh");
    }

    [Fact] void should_not_fail_the_redelivery() => _error.ShouldBeNull();
    [Fact] void should_keep_the_created_document() => _fresh["count"].ToInt32().ShouldEqual(1);
}
