// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Compliance;
using Aksio.Cratis.Kernel.Storage.EventTypes;
using Aksio.Cratis.Kernel.Storage.Identities;

namespace Aksio.Cratis.Kernel.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IEventConverter"/>.
/// </summary>
public class EventConverter : IEventConverter
{
    readonly EventStoreNamespaceName _namespace;
    readonly IEventTypesStorage _eventTypesStorage;
    readonly IIdentityStorage _identityStorage;
    readonly IJsonComplianceManager _jsonComplianceManager;
    readonly Json.IExpandoObjectConverter _expandoObjectConverter;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventConverter"/> class.
    /// </summary>
    /// <param name="namespace"><see cref="TenantId"/> for the converter.</param>
    /// <param name="eventTypesStorage"><see cref="IEventTypesStorage"/> for event schemas.</param>
    /// <param name="identityStorage"><see cref="IIdentityStorage"/>.</param>
    /// <param name="jsonComplianceManager"><see cref="IJsonComplianceManager"/> for handling compliance on events.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between json and expando object.</param>
    public EventConverter(
        EventStoreNamespaceName @namespace,
        IEventTypesStorage eventTypesStorage,
        IIdentityStorage identityStorage,
        IJsonComplianceManager jsonComplianceManager,
        Json.IExpandoObjectConverter expandoObjectConverter)
    {
        _namespace = @namespace;
        _eventTypesStorage = eventTypesStorage;
        _identityStorage = identityStorage;
        _jsonComplianceManager = jsonComplianceManager;
        _expandoObjectConverter = expandoObjectConverter;
    }

    /// <inheritdoc/>
    public async Task<AppendedEvent> ToAppendedEvent(Event @event)
    {
        var eventType = new EventType(@event.Type, EventGeneration.First);
        var content = (JsonNode.Parse(@event.Content[EventGeneration.First.ToString()].ToString()) as JsonObject)!;
        var eventSchema = await _eventTypesStorage.GetFor(eventType.Id, eventType.Generation);
        var releasedContent = await _jsonComplianceManager.Release(eventSchema.Schema, @event.EventSourceId, content);

        var releasedContentAsExpandoObject = _expandoObjectConverter.ToExpandoObject(releasedContent, eventSchema.Schema);

        return new AppendedEvent(
            new(@event.SequenceNumber, eventType),
            new(
                @event.EventSourceId,
                @event.SequenceNumber,
                @event.Occurred,
                @event.ValidFrom,
                (TenantId)(string)_namespace,
                @event.CorrelationId,
                @event.Causation,
                await _identityStorage.GetFor(@event.CausedBy)),
            releasedContentAsExpandoObject);
    }
}
