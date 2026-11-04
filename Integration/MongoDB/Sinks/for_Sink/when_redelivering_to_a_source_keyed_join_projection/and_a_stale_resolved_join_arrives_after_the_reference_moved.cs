// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using MongoDB.Bson;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_redelivering_to_a_source_keyed_join_projection;

[Collection(MongoDBCollection.Name)]
public class and_a_stale_resolved_join_arrives_after_the_reference_moved(ChronicleInProcessFixture fixture) : given.a_source_keyed_join_projection(fixture)
{
    BsonDocument _before;
    BsonDocument _after;

    async Task Because()
    {
        using var sink = CreateSink(UnfencedSinkWrites.Instance);
        await Apply(sink, OwnEvent(30UL), DirectText(("joinId", "b2")));
        _before = await Read("root");
        await Apply(sink, OwnEvent(20UL), Direct(("x", 1)), ResolvedFor(JoinedStream, JoinEvent(5UL), ("count", 99)));
        _after = await Read("root");
    }

    [Fact] void should_keep_the_reference_it_moved_to() => _after["joinId"].AsString.ShouldEqual("b2");
    [Fact] void should_not_write_the_stale_join_data() => _after["count"].ShouldEqual(_before["count"]);
}
