using System.Collections.Immutable;
// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Engines.Compliance;
using Aksio.Cratis.Schemas;

namespace Aksio.Cratis.Kernel.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IEventConverter"/>.
/// </summary>
public class EventConverter : IEventConverter
{
    readonly ISchemaStore _schemaStore;
    readonly IExecutionContextManager _executionContextManager;
    readonly IJsonComplianceManager _jsonComplianceManager;
    readonly Json.IExpandoObjectConverter _expandoObjectConverter;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventConverter"/> class.
    /// </summary>
    /// <param name="schemaStore"><see cref="ISchemaStore"/> for event schemas.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="jsonComplianceManager"><see cref="IJsonComplianceManager"/> for handling compliance on events.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between json and expando object.</param>
    public EventConverter(
        ISchemaStore schemaStore,
        IExecutionContextManager executionContextManager,
        IJsonComplianceManager jsonComplianceManager,
        Json.IExpandoObjectConverter expandoObjectConverter)
    {
        _schemaStore = schemaStore;
        _executionContextManager = executionContextManager;
        _jsonComplianceManager = jsonComplianceManager;
        _expandoObjectConverter = expandoObjectConverter;
    }

    /// <inheritdoc/>
    public async Task<AppendedEvent> ToAppendedEvent(Event @event)
    {
        var eventType = new EventType(@event.Type, EventGeneration.First);
        var content = (JsonNode.Parse(@event.Content[EventGeneration.First.ToString()].ToString()) as JsonObject)!;
        var eventSchema = await _schemaStore.GetFor(eventType.Id, eventType.Generation);
        var releasedContent = await _jsonComplianceManager.Release(eventSchema.Schema, @event.EventSourceId, content);

        var releasedContentAsExpandoObject = _expandoObjectConverter.ToExpandoObject(releasedContent, eventSchema.Schema);

        return new AppendedEvent(
            new(@event.SequenceNumber, eventType),
            new(
                @event.EventSourceId,
                @event.SequenceNumber,
                @event.Occurred,
                @event.ValidFrom,
                _executionContextManager.Current.TenantId,
                @event.CorrelationId,
                @event.Causation,
                @event.CausedBy),
            releasedContentAsExpandoObject);
    }
}
