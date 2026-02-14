// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;

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
        var eventType = new EventType(@event.Type, EventTypeGeneration.First, false);
        var generationKey = EventTypeGeneration.First.ToString();

        JsonObject content;
        if (@event.Compensations.Any())
        {
            var latestCompensation = @event.Compensations.Last();
            generationKey = latestCompensation.EventTypeGeneration.ToString();
            eventType = new EventType(@event.Type, latestCompensation.EventTypeGeneration, false);
            content = (JsonNode.Parse(latestCompensation.Content[generationKey].ToString()) as JsonObject)!;
        }
        else
        {
            content = (JsonNode.Parse(@event.Content[generationKey].ToString()) as JsonObject)!;
        }

        var eventSchema = await eventTypesStorage.GetFor(eventType.Id, eventType.Generation);
        var releasedContent = await jsonComplianceManager.Release(
            eventStoreName,
            eventStoreNamespace,
            eventSchema.Schema,
            @event.EventSourceId,
            content);

        var releasedContentAsExpandoObject = expandoObjectConverter.ToExpandoObject(releasedContent, eventSchema.Schema);
        var hash = @event.ContentHashes.TryGetValue(EventTypeGeneration.First.ToString(), out var hashValue)
            ? new EventHash(hashValue)
            : EventHash.NotSet;

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
            releasedContentAsExpandoObject);
    }
}
