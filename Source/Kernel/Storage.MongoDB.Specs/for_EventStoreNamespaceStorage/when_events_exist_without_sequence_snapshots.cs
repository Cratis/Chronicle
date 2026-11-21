// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.for_EventStoreNamespaceStorage;

public class when_events_exist_without_sequence_snapshots : given.a_namespace_database
{
    bool _hasEvents;

    void Establish()
    {
        _names.AddRange([WellKnownCollectionNames.EventLog, "custom-events", "later-events"]);
        _nonEmpty.Add("custom-events");
        _nonEmpty.Add("later-events");
    }

    async Task Because() => _hasEvents = await _database.HasEvents();

    [Fact] void should_find_custom_history_without_a_snapshot() => _hasEvents.ShouldBeTrue();
    [Fact] void should_stop_after_the_first_nonempty_collection() => _probes.ShouldContainOnly(WellKnownCollectionNames.EventLog, "custom-events");
    [Fact] void should_dispose_catalog_and_event_cursors() => _cursors.ForEach(_ => _.Received().Dispose());
    [Fact] void should_bound_each_probe_to_one_document() => _findOptions.TrueForAll(_ => _.Limit == 1 && _.BatchSize == 1).ShouldBeTrue();
    [Fact] void should_only_read_identifiers() => _findOptions.TrueForAll(_ => _.Projection.Render(new(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry)).Document.Equals(new BsonDocument("_id", 1))).ShouldBeTrue();
}
