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
public class and_the_redelivery_happens_in_a_bulk_window(ChronicleInProcessFixture fixture) : given.a_source_keyed_join_projection(fixture)
{
    BsonDocument _root;

    async Task Because()
    {
        using var sink = CreateSink(UnfencedSinkWrites.Instance);
        await sink.BeginBulk();
        await Apply(sink, OwnEvent(10UL), Direct(("count", 1)));
        await Apply(sink, JoinEvent(20UL), JoinOf(("count", 3)));
        await Apply(sink, OwnEvent(10UL), Direct(("count", 1)));
        await sink.EndBulk();
        _root = await Read("root");
    }

    [Fact] void should_keep_the_joined_value() => _root["count"].ToInt32().ShouldEqual(3);
}
