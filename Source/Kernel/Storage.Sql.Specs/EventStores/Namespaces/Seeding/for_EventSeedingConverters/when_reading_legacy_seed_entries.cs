// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Seeding;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Seeding.for_EventSeedingConverters;

public class when_reading_legacy_seed_entries : Specification
{
    EventSeedsEntity _entity;
    EventSeeds _result;

    void Establish()
    {
        var entry = JsonSerializer.SerializeToNode(new SeededEventEntry("source", "type", "{}", []))!.AsObject();
        entry.Remove(nameof(SeededEventEntry.EventSourceType));
        entry.Remove(nameof(SeededEventEntry.EventStreamType));
        entry.Remove(nameof(SeededEventEntry.EventStreamId));
        _entity = new EventSeedsEntity
        {
            ByEventTypeJson = new JsonObject { ["type"] = new JsonArray(entry.DeepClone()) }.ToJsonString(),
            ByEventSourceJson = new JsonObject { ["source"] = new JsonArray(entry.DeepClone()) }.ToJsonString()
        };
    }

    void Because() => _result = EventSeedingConverters.ToEventSeeds(_entity, JsonSerializerOptions.Default);

    [Fact] void should_default_source_type() => _result.ByEventType.Values.Single().Single().EventSourceType.ShouldEqual(EventSourceType.Default);
    [Fact] void should_default_stream_type() => _result.ByEventType.Values.Single().Single().EventStreamType.ShouldEqual(EventStreamType.All);
    [Fact] void should_default_stream_id() => _result.ByEventSource.Values.Single().Single().EventStreamId.Value.ShouldEqual(EventStreamId.Default);
}
