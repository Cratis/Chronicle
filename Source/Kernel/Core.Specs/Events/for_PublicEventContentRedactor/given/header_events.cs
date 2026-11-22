// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Events.for_PublicEventContentRedactor.given;

public static class header_events
{
    public static readonly JsonSerializerOptions Options = new();
    public static readonly string[] Sentinels = ["current-header-value", "original-header-value", "revision-one-header-value", "revision-two-header-value", "generation-one-header-value", "generation-three-header-value"];

    public static AppendedEvent[] All() =>
        [.. new[] { typeof(WebhookAdded).GetEventType(), typeof(TargetHeadersSetForWebhook).GetEventType() }
            .SelectMany(type => new uint[] { 1, 3 }.SelectMany(generation => new[] { EventStoreName.System, new EventStoreName("application-store") }
                .Select(store => Create(type with { Generation = generation }, store))))
            .Select((@event, index) => @event with { Context = @event.Context with { SequenceNumber = (ulong)index + 12 } })];

    public static AppendedEvent Create(EventType type, EventStoreName store)
    {
        dynamic content = new ExpandoObject();
        content.targetHeaders = new Dictionary<string, string> { ["X-First"] = Sentinels[0], ["X-Second"] = Sentinels[0] };
        content.name = "unchanged";
        content.moment = DateTimeOffset.Parse("2025-01-02T03:04:05+05:30", System.Globalization.CultureInfo.InvariantCulture);
        return new(EventContext.Empty with { EventType = type, EventStore = store, SequenceNumber = 12 }, (ExpandoObject)content)
        {
            OriginalContent = Json(Sentinels[1]),
            Revisions = [Revision(1, Sentinels[2]), Revision(3, Sentinels[3])],
            GenerationalContent = new Dictionary<int, string> { [1] = Json(Sentinels[4]), [3] = Json(Sentinels[5]) },
            GenerationalHashes = new Dictionary<int, EventHash> { [1] = "durable-hash" },
            ErasedProperties = ["erased"],
            RevisedGeneration = 3
        };
    }

    public static string Json(string value) => $$"""{"targetHeaders":{"X-First":"{{value}}","X-Second":"{{value}}"},"name":"unchanged"}""";

    public static bool ContainsSentinel(string value) => Sentinels.Any(sentinel => value.Contains(sentinel, StringComparison.Ordinal));

    public static bool HasEmptyHeaders(string content) => JsonElement.Parse(content).GetProperty("targetHeaders").EnumerateObject()
        .All(property => property.Value.GetString() is { Length: 0 });

    static EventRevision Revision(uint generation, string value) => new(generation, CorrelationId.NotSet, [], Concepts.Identities.Identity.System, DateTimeOffset.UnixEpoch, Json(value));
}
