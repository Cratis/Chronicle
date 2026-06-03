// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Seeding;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Seeding;

/// <summary>
/// Converters for event seeding to and from storage.
/// </summary>
public static class EventSeedingConverters
{
    /// <summary>
    /// Convert <see cref="EventSeeds"/> to <see cref="EventSeedsEntity"/>.
    /// </summary>
    /// <param name="eventSeeds">The <see cref="EventSeeds"/> to convert.</param>
    /// <param name="jsonSerializerOptions">The JSON serializer options.</param>
    /// <returns>The converted <see cref="EventSeedsEntity"/>.</returns>
    public static EventSeedsEntity ToEntity(EventSeeds eventSeeds, JsonSerializerOptions jsonSerializerOptions) =>
        new()
        {
            Id = 0,
            ByEventTypeJson = JsonSerializer.Serialize(
                eventSeeds.ByEventType.ToDictionary(k => k.Key.Value, v => v.Value),
                jsonSerializerOptions),
            ByEventSourceJson = JsonSerializer.Serialize(
                eventSeeds.ByEventSource.ToDictionary(k => k.Key.Value, v => v.Value),
                jsonSerializerOptions)
        };

    /// <summary>
    /// Convert <see cref="EventSeedsEntity"/> to <see cref="EventSeeds"/>.
    /// </summary>
    /// <param name="entity">The <see cref="EventSeedsEntity"/> to convert.</param>
    /// <param name="jsonSerializerOptions">The JSON serializer options.</param>
    /// <returns>The converted <see cref="EventSeeds"/>.</returns>
    public static EventSeeds ToEventSeeds(EventSeedsEntity entity, JsonSerializerOptions jsonSerializerOptions)
    {
        var byEventTypeRaw = JsonSerializer.Deserialize<IDictionary<string, IEnumerable<SeededEventEntry>>>(entity.ByEventTypeJson, jsonSerializerOptions)
            ?? new Dictionary<string, IEnumerable<SeededEventEntry>>();
        var byEventType = byEventTypeRaw.ToDictionary<KeyValuePair<string, IEnumerable<SeededEventEntry>>, EventTypeId, IEnumerable<SeededEventEntry>>(
            k => (EventTypeId)k.Key,
            v => v.Value);

        var byEventSourceRaw = JsonSerializer.Deserialize<IDictionary<string, IEnumerable<SeededEventEntry>>>(entity.ByEventSourceJson, jsonSerializerOptions)
            ?? new Dictionary<string, IEnumerable<SeededEventEntry>>();
        var byEventSource = byEventSourceRaw.ToDictionary<KeyValuePair<string, IEnumerable<SeededEventEntry>>, EventSourceId, IEnumerable<SeededEventEntry>>(
            k => (EventSourceId)k.Key,
            v => v.Value);

        return new EventSeeds(byEventType, byEventSource);
    }
}
