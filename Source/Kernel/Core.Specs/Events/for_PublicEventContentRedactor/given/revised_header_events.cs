// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events.EventSequences;

namespace Cratis.Chronicle.Events.for_PublicEventContentRedactor.given;

public static class revised_header_events
{
    public static AppendedEvent[] All() => header_events.Direct().Select(Wrap).ToArray();

    public static string Envelope(EventType type, string content) => JsonSerializer.Serialize(new
    {
        sequence = "system",
        sequenceNumber = 12,
        eventType = new { id = type.Id.Value, generation = type.Generation.Value, tombstone = type.Tombstone },
        content
    });

    static AppendedEvent Wrap(AppendedEvent original)
    {
        var type = original.Context.EventType;
        return original with
        {
            Context = original.Context with { EventType = typeof(EventRevised).GetEventType() with { Generation = type.Generation } },
            Content = JsonSerializer.Deserialize<ExpandoObject>(Envelope(type, JsonSerializer.Serialize(original.Content)))!,
            OriginalContent = Envelope(type, original.OriginalContent),
            Revisions = original.Revisions.Select(revision => revision with { Content = Envelope(type with { Generation = revision.EventTypeGeneration }, revision.Content) }).ToArray(),
            GenerationalContent = original.GenerationalContent.ToDictionary(pair => pair.Key, pair => Envelope(type with { Generation = (uint)pair.Key }, pair.Value))
        };
    }
}
