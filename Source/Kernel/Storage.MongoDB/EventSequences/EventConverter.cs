// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Events;
using Cratis.Kernel.Compliance;
using Cratis.Kernel.Storage.EventTypes;
using Cratis.Kernel.Storage.Identities;

namespace Cratis.Kernel.Storage.MongoDB;

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
        var eventType = new EventType(@event.Type, EventGeneration.First);
        var content = (JsonNode.Parse(@event.Content[EventGeneration.First.ToString()].ToString()) as JsonObject)!;
        var eventSchema = await eventTypesStorage.GetFor(eventType.Id, eventType.Generation);
        var releasedContent = await jsonComplianceManager.Release(
            eventStoreName,
            eventStoreNamespace,
            eventSchema.Schema,
            @event.EventSourceId,
            content);

        var releasedContentAsExpandoObject = expandoObjectConverter.ToExpandoObject(releasedContent, eventSchema.Schema);

        return new AppendedEvent(
            new(@event.SequenceNumber, eventType),
            new(
                @event.EventSourceId,
                @event.SequenceNumber,
                @event.Occurred,
                @event.ValidFrom,
                (TenantId)(string)eventStoreNamespace,
                @event.CorrelationId,
                @event.Causation,
                await identityStorage.GetFor(@event.CausedBy)),
            releasedContentAsExpandoObject);
    }
}
