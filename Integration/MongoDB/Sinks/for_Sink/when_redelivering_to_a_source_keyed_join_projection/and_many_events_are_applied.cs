// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using MongoDB.Bson;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_redelivering_to_a_source_keyed_join_projection;

[Collection(MongoDBCollection.Name)]
public class and_many_events_are_applied(ChronicleInProcessFixture fixture) : given.a_source_keyed_join_projection(fixture)
{
    BsonDocument _root;

    async Task Because()
    {
        using var sink = CreateSink(UnfencedSinkWrites.Instance);
        for (var index = 1UL; index <= 30UL; index++)
        {
            await Apply(sink, OwnEvent(index * 2), Direct(("count", (int)index)));
            await Apply(sink, JoinEvent((index * 2) + 1), JoinOf(("x", (int)index)));
        }

        _root = await Read("root");
    }

    [Fact] void should_keep_one_own_slot() => _root[Applied]["f"].AsBsonDocument.ElementCount.ShouldEqual(1);
    [Fact] void should_keep_one_join_slot() => _root[Applied]["j"].AsBsonDocument.ElementCount.ShouldEqual(1);
}
