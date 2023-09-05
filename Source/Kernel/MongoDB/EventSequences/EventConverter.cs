// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Events;
using Aksio.Cratis.Identities;
using Aksio.Cratis.Kernel.Engines.Compliance;
using Aksio.Cratis.Kernel.Schemas;
using Aksio.DependencyInversion;

namespace Aksio.Cratis.Kernel.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IEventConverter"/>.
/// </summary>
public class EventConverter : IEventConverter
{
    readonly ProviderFor<ISchemaStore> _schemaStoreProvider;
    readonly ProviderFor<IIdentityStore> _identityStoreProvider;
    readonly IExecutionContextManager _executionContextManager;
    readonly IJsonComplianceManager _jsonComplianceManager;
    readonly Json.IExpandoObjectConverter _expandoObjectConverter;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventConverter"/> class.
    /// </summary>
    /// <param name="schemaStoreProvider">Provider for <see cref="ISchemaStore"/> for event schemas.</param>
    /// <param name="identityStoreProvider">Provider for <see cref="IIdentityStore"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="jsonComplianceManager"><see cref="IJsonComplianceManager"/> for handling compliance on events.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between json and expando object.</param>
    public EventConverter(
        ProviderFor<ISchemaStore> schemaStoreProvider,
        ProviderFor<IIdentityStore> identityStoreProvider,
        IExecutionContextManager executionContextManager,
        IJsonComplianceManager jsonComplianceManager,
        Json.IExpandoObjectConverter expandoObjectConverter)
    {
        _schemaStoreProvider = schemaStoreProvider;
        _identityStoreProvider = identityStoreProvider;
        _executionContextManager = executionContextManager;
        _jsonComplianceManager = jsonComplianceManager;
        _expandoObjectConverter = expandoObjectConverter;
    }

    /// <inheritdoc/>
    public async Task<AppendedEvent> ToAppendedEvent(Event @event)
    {
        var eventType = new EventType(@event.Type, EventGeneration.First);
        var content = (JsonNode.Parse(@event.Content[EventGeneration.First.ToString()].ToString()) as JsonObject)!;
        var eventSchema = await _schemaStoreProvider().GetFor(eventType.Id, eventType.Generation);
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
                await _identityStoreProvider().GetFor(@event.CausedBy)),
            releasedContentAsExpandoObject);
    }
}
