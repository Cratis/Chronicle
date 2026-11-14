// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using MongoDB.Bson;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_redelivering_to_a_source_keyed_join_projection;

[Collection(MongoDBCollection.Name)]
public class and_a_resolved_join_covers_an_older_join(ChronicleInProcessFixture fixture) : given.a_source_keyed_join_projection(fixture)
{
    BsonDocument _root;
    BsonDocument _slotBeforeTheRedelivery;

    async Task Because()
    {
        using var sink = CreateSink(UnfencedSinkWrites.Instance);
        await Apply(sink, JoinEvent(20UL), JoinOf(("count", 3)));
        await Apply(sink, OwnEvent(15UL), Direct(("x", 1)), ResolvedOf(JoinEvent(10UL), ("count", 2)));
        _slotBeforeTheRedelivery = await Read("root");
        await Apply(sink, JoinEvent(20UL), JoinOf(("count", 3)));
        _root = await Read("root");
    }

    /// <summary>
    /// A resolved join of an older event must neither lower the slot nor overwrite the data of the newer join.
    /// </summary>
    [Fact] void should_not_lower_the_join_slot() => _slotBeforeTheRedelivery[Applied]["j"].AsBsonDocument.Single().Value["n"].ToInt64().ShouldEqual(20);
    [Fact] void should_not_let_the_older_resolved_join_overwrite_the_newer_data() => _root["count"].ToInt32().ShouldEqual(3);
    [Fact] void should_still_apply_the_own_write_of_its_event() => _root["x"].ToInt32().ShouldEqual(1);
}
