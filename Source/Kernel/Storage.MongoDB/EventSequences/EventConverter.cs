// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using MongoDB.Bson;
using ConceptsEventCompensation = Cratis.Chronicle.Concepts.Events.EventCompensation;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IEventConverter"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventConverter"/> class.
/// </remarks>
/// <param name="eventStoreName"><see cref="EventStoreName"/> the converter is for.</param>
/// <param name="eventStoreNamespace"><see cref="EventStoreNamespaceName"/> the converter is for.</param>
/// <param name="eventTypesStorage"><see cref="IEventTypesStorage"/> for event schemas.</param>
/// <param name="identityStorage"><see cref="IIdentityStorage"/>.</param>
/// <param name="jsonComplianceManager"><see cref="IJsonComplianceManager"/> for handling compliance on events.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between json and expando object.</param>
public class EventConverter(
    EventStoreName eventStoreName,
    EventStoreNamespaceName eventStoreNamespace,
    IEventTypesStorage eventTypesStorage,
    IIdentityStorage identityStorage,
    IJsonComplianceManager jsonComplianceManager,
    Json.IExpandoObjectConverter expandoObjectConverter) : IEventConverter
{
    /// <inheritdoc/>
    public async Task<AppendedEvent> ToAppendedEvent(Event @event)
    {
        var (eventType, generationKey, content) = ExtractContent(@event);
        var hash = ExtractHash(@event, generationKey);
        var resolvedContent = await ResolveContent(eventType, @event.EventSourceId, content);
        var compensations = await ResolveCompensations(@event);

        var originalContent = @event.Compensations.Any() && @event.Content.TryGetValue(EventTypeGeneration.First.ToString(), out var originalBson)
            ? originalBson.ToString()
            : string.Empty;

        return new AppendedEvent(
            new(
                eventType,
                @event.EventSourceType,
                @event.EventSourceId,
                @event.EventStreamType,
                @event.EventStreamId,
                @event.SequenceNumber,
                @event.Occurred,
                eventStoreName,
                eventStoreNamespace,
                @event.CorrelationId,
                @event.Causation,
                await identityStorage.GetFor(@event.CausedBy),
                @event.Tags.Select(_ => new Tag(_)).ToArray(),
                hash),
            resolvedContent)
        {
            OriginalContent = originalContent,
            Compensations = compensations
        };
    }

    static EventHash ExtractHash(Event @event, string generationKey)
    {
        if (@event.Compensations.Any())
        {
            var latest = @event.Compensations.Last();
            var key = latest.EventTypeGeneration.ToString();
            return latest.ContentHashes.TryGetValue(key, out var hv) ? new EventHash(hv) : EventHash.NotSet;
        }

        return @event.ContentHashes.TryGetValue(generationKey, out var hashValue) ? new EventHash(hashValue) : EventHash.NotSet;
    }

    static JsonObject ParseContent(IDictionary<string, BsonDocument> content, string generationKey)
        => (JsonNode.Parse(content[generationKey].ToString()) as JsonObject)!;

    static ExpandoObject ConvertToRawExpandoObject(JsonObject document)
    {
        var result = new ExpandoObject();
        var dict = (IDictionary<string, object?>)result;
        foreach (var (key, value) in document)
            dict[key] = ConvertJsonNodeToClrType(value);
        return result;
    }

    static object? ConvertJsonNodeToClrType(JsonNode? node) => node switch
    {
        null => null,
        JsonObject obj => ConvertToRawExpandoObject(obj),
        JsonArray array => array.Select(ConvertJsonNodeToClrType).ToArray(),
        JsonValue value when value.TryGetValue<bool>(out var b) => b,
        JsonValue value when value.TryGetValue<long>(out var l) => l,
        JsonValue value when value.TryGetValue<double>(out var d) => d,
        JsonValue value when value.TryGetValue<string>(out var s) => s,
        _ => node.ToString()
    };

    (EventType EventType, string GenerationKey, JsonObject Content) ExtractContent(Event @event)
    {
        var generationKey = EventTypeGeneration.First.ToString();
        var eventType = new EventType(@event.Type, EventTypeGeneration.First, false);

        if (!@event.Compensations.Any())
            return (eventType, generationKey, ParseContent(@event.Content, generationKey));

        var latest = @event.Compensations.Last();
        var compensationKey = latest.EventTypeGeneration.ToString();
        return (new EventType(@event.Type, latest.EventTypeGeneration, false), compensationKey, ParseContent(latest.Content, compensationKey));
    }

    async Task<ExpandoObject> ResolveContent(EventType eventType, EventSourceId eventSourceId, JsonObject content)
    {
        if (!await eventTypesStorage.HasFor(eventType.Id, eventType.Generation))
            return ConvertToRawExpandoObject(content);

        var schema = await eventTypesStorage.GetFor(eventType.Id, eventType.Generation);
        var released = await jsonComplianceManager.Release(eventStoreName, eventStoreNamespace, schema.Schema, eventSourceId, content);
        return expandoObjectConverter.ToExpandoObject(released, schema.Schema);
    }

    async Task<IEnumerable<ConceptsEventCompensation>> ResolveCompensations(Event @event)
    {
        var result = new List<ConceptsEventCompensation>();
        foreach (var compensation in @event.Compensations)
        {
            var causedBy = await identityStorage.GetFor([compensation.CausedBy]);
            var compensationKey = compensation.EventTypeGeneration.ToString();
            var compensationContentJson = compensation.Content.TryGetValue(compensationKey, out var contentBson)
                ? contentBson.ToString()
                : string.Empty;

            result.Add(new ConceptsEventCompensation(
                compensation.EventTypeGeneration,
                compensation.CorrelationId,
                compensation.Causation,
                causedBy,
                compensation.Occurred,
                compensationContentJson));
        }

        return result;
    }
}
