// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using MongoDB.Bson;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_redelivering_to_a_source_keyed_join_projection;

[Collection(MongoDBCollection.Name)]
public class and_the_reference_returns_to_an_earlier_join_target(ChronicleInProcessFixture fixture) : given.a_source_keyed_join_projection(fixture)
{
    BsonDocument _root;

    async Task Because()
    {
        using var sink = CreateSink(UnfencedSinkWrites.Instance);
        await Apply(sink, JoinEvent(20UL), JoinOf(("count", 20)));
        await Apply(sink, OwnEvent(30UL), DirectText(("joinId", "b2")), ResolvedFor("b2", EventOf("joined", "b2", 7UL), ("count", 7)));
        await Apply(sink, OwnEvent(40UL), DirectText(("joinId", JoinedStream)), ResolvedFor(JoinedStream, JoinEvent(20UL), ("count", 20)));
        _root = await Read("root");
    }

    [Fact] void should_fill_the_data_of_the_target_it_returned_to() => _root["count"].ToInt32().ShouldEqual(20);
    [Fact] void should_reference_the_target_again() => _root["joinId"].AsString.ShouldEqual(JoinedStream);
    [Fact] void should_record_the_join_event_in_the_slot() => SlotOf(_root)["n"].ToInt64().ShouldEqual(20);
}
