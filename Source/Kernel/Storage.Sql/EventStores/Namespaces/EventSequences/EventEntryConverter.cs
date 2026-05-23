// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Converter for SQL Event entities to and from domain models.
/// </summary>
public static class EventEntryConverter
{
    static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Convert from domain models to <see cref="EventEntry"/>.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number.</param>
    /// <param name="eventSourceType">The event source type.</param>
    /// <param name="eventSourceId">The event source identifier.</param>
    /// <param name="eventStreamType">The event stream type.</param>
    /// <param name="eventStreamId">The event stream identifier.</param>
    /// <param name="eventType">The event type.</param>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="causation">The causation chain.</param>
    /// <param name="causedByChain">The caused by chain.</param>
    /// <param name="occurred">When the event occurred.</param>
    /// <param name="content">The event content.</param>
    /// <param name="hash">Optional content hash, computed by the kernel.</param>
    /// <returns>The <see cref="EventEntry"/>.</returns>
    public static EventEntry ToEventEntry(
        EventSequenceNumber sequenceNumber,
        EventSourceType eventSourceType,
        EventSourceId eventSourceId,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId,
        EventType eventType,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        DateTimeOffset occurred,
        ExpandoObject content,
        EventHash? hash = null)
    {
        var contentDict = new Dictionary<string, object>
        {
            { eventType.Generation.ToString(), content }
        };

        var contentHashesJson = hash is not null && !string.IsNullOrEmpty(hash.Value)
            ? JsonSerializer.Serialize(
                new Dictionary<string, string> { { ((uint)eventType.Generation).ToString(), hash.Value } },
                _jsonSerializerOptions)
            : string.Empty;

        return new EventEntry
        {
            SequenceNumber = sequenceNumber.Value,
            CorrelationId = correlationId.ToString(),
            Causation = JsonSerializer.Serialize(causation, _jsonSerializerOptions),
            CausedBy = JsonSerializer.Serialize(causedByChain.Select(id => id.ToString()), _jsonSerializerOptions),
            Type = eventType.Id,
            Occurred = occurred,
            EventSourceType = eventSourceType,
            EventSourceId = eventSourceId,
            EventStreamType = eventStreamType,
            EventStreamId = eventStreamId,
            Content = JsonSerializer.Serialize(contentDict, _jsonSerializerOptions),
            ContentHashes = contentHashesJson,
            Compensations = new Dictionary<string, string>()
        };
    }

    /// <summary>
    /// Convert from domain models (multi-generation content) to <see cref="EventEntry"/>.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number.</param>
    /// <param name="eventSourceType">The event source type.</param>
    /// <param name="eventSourceId">The event source identifier.</param>
    /// <param name="eventStreamType">The event stream type.</param>
    /// <param name="eventStreamId">The event stream identifier.</param>
    /// <param name="eventType">The event type.</param>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="causation">The causation chain.</param>
    /// <param name="causedByChain">The caused by chain.</param>
    /// <param name="occurred">When the event occurred.</param>
    /// <param name="content">The event content per generation.</param>
    /// <param name="contentHashes">Optional hash per generation, computed by the kernel.</param>
    /// <param name="subject">Optional subject identifying the compliance target.</param>
    /// <returns>The <see cref="EventEntry"/>.</returns>
    public static EventEntry ToEventEntry(
        EventSequenceNumber sequenceNumber,
        EventSourceType eventSourceType,
        EventSourceId eventSourceId,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId,
        EventType eventType,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        DateTimeOffset occurred,
        IDictionary<EventTypeGeneration, ExpandoObject> content,
        IDictionary<EventTypeGeneration, EventHash>? contentHashes = null,
        Subject? subject = null)
    {
        var contentDict = content.ToDictionary(
            kvp => ((uint)kvp.Key).ToString(),
            kvp => (object)kvp.Value);

        var contentHashesJson = contentHashes is { Count: > 0 }
            ? JsonSerializer.Serialize(
                contentHashes.ToDictionary(kvp => ((uint)kvp.Key).ToString(), kvp => kvp.Value.Value),
                _jsonSerializerOptions)
            : string.Empty;

        return new EventEntry
        {
            SequenceNumber = sequenceNumber.Value,
            CorrelationId = correlationId.ToString(),
            Causation = JsonSerializer.Serialize(causation, _jsonSerializerOptions),
            CausedBy = JsonSerializer.Serialize(causedByChain.Select(id => id.ToString()), _jsonSerializerOptions),
            Type = eventType.Id,
            Occurred = occurred,
            EventSourceType = eventSourceType,
            EventSourceId = eventSourceId,
            EventStreamType = eventStreamType,
            EventStreamId = eventStreamId,
            Content = JsonSerializer.Serialize(contentDict, _jsonSerializerOptions),
            ContentHashes = contentHashesJson,
            Compensations = new Dictionary<string, string>(),
            Subject = subject?.Value
        };
    }

    /// <summary>
    /// Update the content for a specific event type generation in an <see cref="EventEntry"/>.
    /// </summary>
    /// <param name="entry">The event entry to update.</param>
    /// <param name="generation">The generation to update content for.</param>
    /// <param name="content">The new content.</param>
    public static void UpdateContentForGeneration(EventEntry entry, EventTypeGeneration generation, ExpandoObject content)
    {
        var contentDict = string.IsNullOrEmpty(entry.Content)
            ? new Dictionary<string, JsonElement>()
            : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(entry.Content, _jsonSerializerOptions) ?? [];

        contentDict[((uint)generation).ToString()] = JsonSerializer.SerializeToElement(content, _jsonSerializerOptions);
        entry.Content = JsonSerializer.Serialize(contentDict, _jsonSerializerOptions);
    }

    /// <summary>
    /// Replace all generational content in an <see cref="EventEntry"/>.
    /// </summary>
    /// <param name="entry">The event entry to update.</param>
    /// <param name="content">The new content per generation.</param>
    public static void ReplaceAllGenerationContent(EventEntry entry, IDictionary<EventTypeGeneration, ExpandoObject> content)
    {
        var contentDict = content.ToDictionary(
            kvp => ((uint)kvp.Key).ToString(),
            kvp => (object)kvp.Value);

        entry.Content = JsonSerializer.Serialize(contentDict, _jsonSerializerOptions);
    }

    /// <summary>
    /// Resolve the compliance <see cref="Subject"/> for an event entry. Defaults to the event
    /// source identifier when no explicit subject was stored — mirrors the MongoDB backend.
    /// </summary>
    /// <param name="entry">The event entry.</param>
    /// <returns>The resolved <see cref="Subject"/>.</returns>
    public static Subject ResolveSubject(EventEntry entry) =>
        string.IsNullOrEmpty(entry.Subject) ? new Subject(entry.EventSourceId) : new Subject(entry.Subject);

    /// <summary>
    /// Get the stored content hash for a specific generation.
    /// </summary>
    /// <param name="entry">The event entry.</param>
    /// <param name="generation">The generation to read the hash for.</param>
    /// <returns>The <see cref="EventHash"/> for the generation, or <see cref="EventHash.NotSet"/> when none was stored.</returns>
    public static EventHash GetHashForGeneration(EventEntry entry, EventTypeGeneration generation)
    {
        if (string.IsNullOrEmpty(entry.ContentHashes))
        {
            return EventHash.NotSet;
        }

        var hashesByGeneration = JsonSerializer.Deserialize<Dictionary<string, string>>(entry.ContentHashes, _jsonSerializerOptions);
        if (hashesByGeneration is null)
        {
            return EventHash.NotSet;
        }

        return hashesByGeneration.TryGetValue(((uint)generation).ToString(), out var hash)
            ? new EventHash(hash)
            : EventHash.NotSet;
    }

    /// <summary>
    /// Get the event type from an event entry. Selects the highest available generation so
    /// observers and projections subscribed to a newer generation receive the migrated content
    /// by default — mirrors the MongoDB backend.
    /// </summary>
    /// <param name="entry">The event entry.</param>
    /// <returns>The event type.</returns>
    public static EventType GetEventType(EventEntry entry)
    {
        var highestGeneration = GetHighestGeneration(entry);
        return new EventType(entry.Type, new EventTypeGeneration(highestGeneration), false);
    }

    /// <summary>
    /// Get the highest generation stored in an event entry's content.
    /// </summary>
    /// <param name="entry">The event entry.</param>
    /// <returns>The highest generation number, or 1 if none are stored.</returns>
    public static uint GetHighestGeneration(EventEntry entry)
    {
        if (string.IsNullOrEmpty(entry.Content))
        {
            return EventTypeGeneration.First.Value;
        }

        var contentDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(entry.Content, _jsonSerializerOptions);
        if (contentDict is null || contentDict.Count == 0)
        {
            return EventTypeGeneration.First.Value;
        }

        var highest = contentDict.Keys
            .Select(k => uint.TryParse(k, out var g) ? g : 0u)
            .DefaultIfEmpty(EventTypeGeneration.First.Value)
            .Max();
        return highest == 0u ? EventTypeGeneration.First.Value : highest;
    }

    /// <summary>
    /// Get the content for a specific generation from an event entry.
    /// </summary>
    /// <param name="entry">The event entry.</param>
    /// <param name="generation">The generation to get content for.</param>
    /// <returns>The content as <see cref="ExpandoObject"/>.</returns>
    /// <remarks>
    /// The stored content is a JSON object keyed by generation. The deserialized
    /// <see cref="ExpandoObject"/> must contain CLR-typed values (strings, primitives,
    /// nested <see cref="ExpandoObject"/>, <c>object[]</c>) — not <see cref="JsonElement"/> —
    /// because downstream consumers (e.g. <c>ExpandoObjectConverter.ToJsonObject</c>) only
    /// know how to project CLR types back to JSON. The default <c>JsonSerializer.Deserialize&lt;ExpandoObject&gt;</c>
    /// path leaves nested arrays as <see cref="JsonElement"/>, which silently strips them when
    /// the content is re-projected to JSON.
    /// </remarks>
    public static ExpandoObject GetContentForGeneration(EventEntry entry, EventTypeGeneration generation)
    {
        if (string.IsNullOrEmpty(entry.Content))
        {
            return new ExpandoObject();
        }

        var contentDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(entry.Content, _jsonSerializerOptions);
        if (contentDict?.TryGetValue(generation.ToString(), out var contentElement) == true && contentElement.ValueKind == JsonValueKind.Object)
        {
            return ConvertJsonObjectToExpando(contentElement);
        }

        return new ExpandoObject();
    }

    /// <summary>
    /// Build a generational content dictionary from the in-memory content map used during event append.
    /// </summary>
    /// <param name="content">The content per generation as ExpandoObjects.</param>
    /// <returns>Dictionary mapping generation number to serialized JSON content string.</returns>
    public static IReadOnlyDictionary<int, string> BuildGenerationalContent(IDictionary<EventTypeGeneration, ExpandoObject> content)
    {
        var result = new Dictionary<int, string>();
        foreach (var (generation, expandoContent) in content)
        {
            result[(int)generation.Value] = JsonSerializer.Serialize(expandoContent, _jsonSerializerOptions);
        }

        return result;
    }

    /// <summary>
    /// Get all generational content from an event entry as a dictionary keyed by generation number.
    /// </summary>
    /// <param name="entry">The event entry.</param>
    /// <returns>Dictionary mapping generation number to serialized JSON content.</returns>
    public static IReadOnlyDictionary<int, string> GetAllGenerationalContent(EventEntry entry)
    {
        if (string.IsNullOrEmpty(entry.Content))
        {
            return new Dictionary<int, string>();
        }

        var contentDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(entry.Content, _jsonSerializerOptions);
        if (contentDict is null)
        {
            return new Dictionary<int, string>();
        }

        var result = new Dictionary<int, string>();
        foreach (var (key, value) in contentDict)
        {
            if (int.TryParse(key, out var generation))
            {
                result[generation] = value.GetRawText();
            }
        }

        return result;
    }

    /// <summary>
    /// Get the causation chain from an event entry.
    /// </summary>
    /// <param name="entry">The event entry.</param>
    /// <returns>The causation chain.</returns>
    public static IEnumerable<Causation> GetCausation(EventEntry entry)
    {
        if (string.IsNullOrEmpty(entry.Causation))
        {
            return [];
        }

        return JsonSerializer.Deserialize<IEnumerable<Causation>>(entry.Causation, _jsonSerializerOptions) ?? [];
    }

    /// <summary>
    /// Get the caused by chain from an event entry.
    /// </summary>
    /// <param name="entry">The event entry.</param>
    /// <returns>The caused by chain.</returns>
    public static IEnumerable<IdentityId> GetCausedBy(EventEntry entry)
    {
        if (string.IsNullOrEmpty(entry.CausedBy))
        {
            return [];
        }

        var identityStrings = JsonSerializer.Deserialize<IEnumerable<string>>(entry.CausedBy, _jsonSerializerOptions) ?? [];
        return identityStrings.Select(id => new IdentityId(Guid.Parse(id)));
    }

    /// <summary>
    /// Create the serialized redaction content in generation-keyed format, matching the MongoDB storage pattern.
    /// </summary>
    /// <param name="originalEventTypeId">The original event type identifier before redaction.</param>
    /// <param name="reason">The reason for the redaction.</param>
    /// <param name="correlationId">The correlation identifier of the redaction operation.</param>
    /// <param name="causation">The causation chain for the redaction.</param>
    /// <param name="causedByChain">The identity chain that caused the redaction.</param>
    /// <param name="occurred">When the redaction occurred.</param>
    /// <returns>Serialized JSON string with generation "1" key wrapping the redaction content.</returns>
    public static string CreateRedactionContent(
        string originalEventTypeId,
        RedactionReason reason,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        DateTimeOffset occurred)
    {
        var content = new
        {
            reason = reason.Value,
            originalEventType = originalEventTypeId,
            occurred,
            correlationId = correlationId.ToString(),
            causation = causation.Select(c => new { type = c.Type.Value, occurred = c.Occurred }),
            causedBy = causedByChain.Select(id => id.ToString())
        };

        var contentWrapper = new Dictionary<string, object>
        {
            { EventTypeGeneration.First.ToString(), content }
        };

        return JsonSerializer.Serialize(contentWrapper, _jsonSerializerOptions);
    }

    /// <summary>
    /// Serialize a causation chain to JSON.
    /// </summary>
    /// <param name="causation">The causation chain to serialize.</param>
    /// <returns>Serialized JSON string.</returns>
    public static string SerializeCausation(IEnumerable<Causation> causation) =>
        JsonSerializer.Serialize(causation, _jsonSerializerOptions);

    /// <summary>
    /// Serialize a caused-by identity chain to JSON.
    /// </summary>
    /// <param name="causedByChain">The identity chain to serialize.</param>
    /// <returns>Serialized JSON string.</returns>
    public static string SerializeCausedBy(IEnumerable<IdentityId> causedByChain) =>
        JsonSerializer.Serialize(causedByChain.Select(id => id.ToString()), _jsonSerializerOptions);

    static ExpandoObject ConvertJsonObjectToExpando(JsonElement element)
    {
        var expando = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)expando;
        foreach (var property in element.EnumerateObject())
        {
            dictionary[property.Name] = ConvertJsonElementToClrValue(property.Value);
        }
        return expando;
    }

    static object? ConvertJsonElementToClrValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                return ConvertJsonObjectToExpando(element);
            case JsonValueKind.Array:
                var items = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    items.Add(ConvertJsonElementToClrValue(item));
                }

                return items.ToArray();
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var l))
                {
                    return l;
                }

                return element.GetDouble();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            default:
                return null;
        }
    }
}
