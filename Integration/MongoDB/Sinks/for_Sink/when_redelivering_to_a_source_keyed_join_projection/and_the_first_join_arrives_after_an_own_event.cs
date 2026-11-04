// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using MongoDB.Bson;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_redelivering_to_a_source_keyed_join_projection;

[Collection(MongoDBCollection.Name)]
public class and_the_first_join_arrives_after_an_own_event(ChronicleInProcessFixture fixture) : given.a_source_keyed_join_projection(fixture)
{
    BsonDocument _root;

    async Task Because()
    {
        using var sink = CreateSink(UnfencedSinkWrites.Instance);
        await Apply(sink, OwnEvent(20UL), Direct(("count", 1)));
        await Apply(sink, JoinEvent(5UL), JoinOf(("count", 2)));
        _root = await Read("root");
    }

    [Fact] void should_apply_the_join() => _root["count"].ToInt32().ShouldEqual(2);
}
