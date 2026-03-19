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
        ExpandoObject content)
    {
        var contentDict = new Dictionary<string, object>
        {
            { eventType.Generation.ToString(), content }
        };

        return new EventEntry
        {
            SequenceNumber = sequenceNumber.Value,
            CorrelationId = correlationId.ToString(),
            Causation = JsonSerializer.Serialize(causation, _jsonSerializerOptions),
            CausedBy = JsonSerializer.Serialize(causedByChain.Select(id => id.ToString()), _jsonSerializerOptions),
            Type = eventType.Id.Value,
            Occurred = occurred,
            EventSourceType = eventSourceType.Value,
            EventSourceId = eventSourceId.Value,
            EventStreamType = eventStreamType.Value,
            EventStreamId = eventStreamId.Value,
            Content = JsonSerializer.Serialize(contentDict, _jsonSerializerOptions),
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
        IDictionary<EventTypeGeneration, ExpandoObject> content)
    {
        var contentDict = content.ToDictionary(
            kvp => ((uint)kvp.Key).ToString(),
            kvp => (object)kvp.Value);

        return new EventEntry
        {
            SequenceNumber = sequenceNumber.Value,
            CorrelationId = correlationId.ToString(),
            Causation = JsonSerializer.Serialize(causation, _jsonSerializerOptions),
            CausedBy = JsonSerializer.Serialize(causedByChain.Select(id => id.ToString()), _jsonSerializerOptions),
            Type = eventType.Id.Value,
            Occurred = occurred,
            EventSourceType = eventSourceType.Value,
            EventSourceId = eventSourceId.Value,
            EventStreamType = eventStreamType.Value,
            EventStreamId = eventStreamId.Value,
            Content = JsonSerializer.Serialize(contentDict, _jsonSerializerOptions),
            Compensations = new Dictionary<string, string>()
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
            : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(entry.Content, _jsonSerializerOptions) ?? new Dictionary<string, JsonElement>();

        var contentElement = JsonSerializer.SerializeToElement(content, _jsonSerializerOptions);
        contentDict[((uint)generation).ToString()] = contentElement;
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
    /// Get the event type from an event entry.
    /// </summary>
    /// <param name="entry">The event entry.</param>
    /// <returns>The event type.</returns>
    public static EventType GetEventType(EventEntry entry)
    {
        return new EventType(new EventTypeId(entry.Type), EventTypeGeneration.First, false);
    }

    /// <summary>
    /// Get the content for a specific generation from an event entry.
    /// </summary>
    /// <param name="entry">The event entry.</param>
    /// <param name="generation">The generation to get content for.</param>
    /// <returns>The content as ExpandoObject.</returns>
    public static ExpandoObject GetContentForGeneration(EventEntry entry, EventTypeGeneration generation)
    {
        if (string.IsNullOrEmpty(entry.Content))
        {
            return new ExpandoObject();
        }

        var contentDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(entry.Content, _jsonSerializerOptions);
        if (contentDict?.TryGetValue(generation.ToString(), out var contentElement) == true)
        {
            var contentAsObject = JsonSerializer.Deserialize<ExpandoObject>(contentElement.GetRawText(), _jsonSerializerOptions);
            return contentAsObject ?? new ExpandoObject();
        }

        return new ExpandoObject();
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
}
