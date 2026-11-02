// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using MongoDB.Bson;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_redelivering_to_a_source_keyed_join_projection;

[Collection(MongoDBCollection.Name)]
public class and_two_join_definitions_write_to_one_document(ChronicleInProcessFixture fixture) : given.a_source_keyed_join_projection(fixture)
{
    BsonDocument _root;

    async Task Because()
    {
        using var sink = CreateSink(UnfencedSinkWrites.Instance);
        await Apply(sink, JoinEvent(10UL, "joined"), JoinOf(("count", 1)));
        await Apply(sink, JoinEvent(20UL, "also-joined"), JoinOf(("x", 5)));
        await Apply(sink, JoinEvent(10UL, "joined"), JoinOf(("count", 1)));
        _root = await Read("root");
    }

    [Fact] void should_keep_one_slot_per_definition() => _root[Applied]["j"].AsBsonDocument.ElementCount.ShouldEqual(2);
    [Fact] void should_keep_the_other_definitions_value() => _root["x"].ToInt32().ShouldEqual(5);
}
