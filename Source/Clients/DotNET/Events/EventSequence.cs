// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Connections;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Events;

/// <summary>
/// Represents the payload for appending an event.
/// </summary>
/// <param name="EventSourceId"><see cref="EventSourceId"/> for the event to append to.</param>
/// <param name="EventType">The <see cref="EventType"/> to append.</param>
/// <param name="Content">The content to of the event append.</param>
/// <param name="ValidFrom">Optional valid from.</param>
public record AppendEvent(EventSourceId EventSourceId, EventType EventType, JsonObject Content, DateTimeOffset? ValidFrom);

/// <summary>
/// Represents an implementation of <see cref="IEventSequence"/>,
/// </summary>
public class EventSequence : IEventSequence
{
    readonly EventSequenceId _eventSequenceId;
    readonly IEventTypes _eventTypes;
    readonly IEventSerializer _eventSerializer;
    readonly IConnectionFactory _connectionFactory;
    readonly IExecutionContextManager _executionContextManager;

    public EventSequence(
        EventSequenceId eventSequenceId,
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        IConnectionFactory connectionFactory,
        IExecutionContextManager executionContextManager)
    {
        _eventSequenceId = eventSequenceId;
        _eventTypes = eventTypes;
        _eventSerializer = eventSerializer;
        _connectionFactory = connectionFactory;
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public async Task Append(EventSourceId eventSourceId, object @event, DateTimeOffset? validFrom = null)
    {
        var connection = await _connectionFactory.GetConnection();
        var tenantId = _executionContextManager.Current.TenantId;
        var microserviceId = _executionContextManager.Current.MicroserviceId;
        var eventType = _eventTypes.GetEventTypeFor(@event.GetType());
        var serializedEvent = await _eventSerializer.Serialize(@event);
        var payload = new AppendEvent(eventSourceId, eventType, serializedEvent, validFrom);
        var route = $"/api/events/store/{microserviceId}/{tenantId}/sequence/{_eventSequenceId}";
        await connection.PerformCommand(route, payload);
    }
}
