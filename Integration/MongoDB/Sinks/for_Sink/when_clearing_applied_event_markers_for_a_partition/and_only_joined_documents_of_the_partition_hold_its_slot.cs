// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using MongoDB.Bson;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_clearing_applied_event_markers_for_a_partition;

[Collection(MongoDBCollection.Name)]
public class and_only_joined_documents_of_the_partition_hold_its_slot(ChronicleInProcessFixture fixture) : given.a_partition_with_markers(fixture)
{
    BsonDocument _root;
    BsonDocument _other;
    BsonDocument _third;

    async Task Because()
    {
        await _documents.InsertOneAsync(new BsonDocument { ["_id"] = "third", ["joinId"] = "b2", ["count"] = 0 });
        using var sink = CreateSink(UnfencedSinkWrites.Instance);
        await Apply(sink, JoinEvent(10UL), JoinOf(("count", 2)));
        await Apply(sink, EventOf("joined", "b2", 11UL), new Joined(new ExpandoObject(), "b2", new PropertyPath("joinId"), ArrayIndexers.NoIndexers, [Direct(("count", 5))]));
        await Clear(sink, JoinedStream);
        _root = await Read("root");
        _other = await Read("other");
        _third = await Read("third");
    }

    [Fact] void should_clear_the_slot_on_the_root() => HasJoinSlot(_root).ShouldBeFalse();
    [Fact] void should_clear_the_slot_on_the_other_row() => HasJoinSlot(_other).ShouldBeFalse();
    [Fact] void should_keep_the_slot_of_the_other_stream() => HasJoinSlot(_third).ShouldBeTrue();
    [Fact] void should_keep_the_values() => _root["count"].ToInt32().ShouldEqual(2);
}
