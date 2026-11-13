// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using MongoDB.Bson;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_redelivering_to_a_source_keyed_join_projection;

[Collection(MongoDBCollection.Name)]
public class and_a_from_and_a_join_change_the_same_property(ChronicleInProcessFixture fixture) : given.a_source_keyed_join_projection(fixture)
{
    BsonDocument _written;
    BsonDocument _redelivered;
    BsonDocument _afterNewerWrite;

    async Task Because()
    {
        using var sink = CreateSink(UnfencedSinkWrites.Instance);
        await Apply(sink, OwnEvent(10UL), Direct(("count", 1)), JoinOf(("count", 2)) with { HasKeyedFrom = true });
        _written = await Read("root");
        await Apply(sink, OwnEvent(10UL), Direct(("count", 1)), JoinOf(("count", 2)) with { HasKeyedFrom = true });
        _redelivered = await Read("root");
        await Apply(sink, OwnEvent(20UL), Direct(("count", 7)));
        await Apply(sink, OwnEvent(10UL), Direct(("count", 1)), JoinOf(("count", 2)) with { HasKeyedFrom = true });
        _afterNewerWrite = await Read("root");
    }

    [Fact] void should_end_with_the_value_of_the_change_that_comes_last() => _written["count"].ToInt32().ShouldEqual(2);
    [Fact] void should_not_change_the_document_on_a_redelivery() => _redelivered["count"].ToInt32().ShouldEqual(2);
    [Fact] void should_not_let_a_redelivered_event_overwrite_a_newer_write() => _afterNewerWrite["count"].ToInt32().ShouldEqual(7);
}
