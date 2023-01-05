// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventSequence"/>,
/// </summary>
public class EventSequence : IEventSequence
{
    readonly EventSequenceId _eventSequenceId;
    readonly IEventTypes _eventTypes;
    readonly IEventSerializer _eventSerializer;
    readonly IClient _client;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequence"/> class.
    /// </summary>
    /// <param name="eventSequenceId">The event sequence it represents.</param>
    /// <param name="eventTypes">Known <see cref="IEventTypes"/>.</param>
    /// <param name="eventSerializer">The <see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="client"><see cref="IClient"/> for getting connections.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public EventSequence(
        EventSequenceId eventSequenceId,
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        IClient client,
        IExecutionContextManager executionContextManager)
    {
        _eventSequenceId = eventSequenceId;
        _eventTypes = eventTypes;
        _eventSerializer = eventSerializer;
        _client = client;
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public async Task Append(EventSourceId eventSourceId, object @event, DateTimeOffset? validFrom = null)
    {
        var tenantId = _executionContextManager.Current.TenantId;
        var microserviceId = _executionContextManager.Current.MicroserviceId;
        var eventType = _eventTypes.GetEventTypeFor(@event.GetType());
        var serializedEvent = await _eventSerializer.Serialize(@event);
        var payload = new AppendEvent(eventSourceId, eventType, serializedEvent, validFrom);
        var route = $"/api/events/store/{microserviceId}/{tenantId}/sequence/{_eventSequenceId}";
        await _client.PerformCommand(route, payload);
    }
}
