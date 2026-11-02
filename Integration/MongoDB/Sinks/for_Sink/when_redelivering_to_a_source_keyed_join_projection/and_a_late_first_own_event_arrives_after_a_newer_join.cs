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
public class and_a_late_first_own_event_arrives_after_a_newer_join(ChronicleInProcessFixture fixture) : given.a_source_keyed_join_projection(fixture)
{
    BsonDocument _root;

    async Task Because()
    {
        using var sink = CreateSink(UnfencedSinkWrites.Instance);
        await Apply(sink, JoinEvent(20UL), JoinOf(("count", 3)));
        await Apply(sink, OwnEvent(5UL), Direct(("count", 1)));
        _root = await Read("root");
    }

    // An event delivered for the first time is always applied, even when it is older than other writes to the document.
    [Fact] void should_apply_the_own_event() => _root["count"].ToInt32().ShouldEqual(1);
}
