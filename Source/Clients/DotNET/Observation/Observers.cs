// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Observation;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObservers"/>.
/// </summary>
public class Observers : IObservers
{
    readonly IConnection _connection;
    readonly IEventTypes _eventTypes;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observers"/> class.
    /// </summary>
    /// <param name="connection"><see cref="IConnection"/> for getting connections.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for resolving event types.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public Observers(
        IConnection connection,
        IEventTypes eventTypes,
        IExecutionContextManager executionContextManager)
    {
        _connection = connection;
        _eventTypes = eventTypes;
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ObserverInformation>> GetAllObservers()
    {
        var tenantId = _executionContextManager.Current.TenantId;
        var microserviceId = _executionContextManager.Current.MicroserviceId;
        var route = $"/api/events/store/{microserviceId}/{tenantId}/observers";
        var result = await _connection.PerformQuery<IEnumerable<ObserverInformation>>(route);
        return result.Data;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ObserverInformation>> GetObserversForEventTypes(IEnumerable<Type> eventTypes)
    {
        var observers = await GetAllObservers();
        var eventTypeIdentifiers = eventTypes.Select(_eventTypes.GetEventTypeFor);
        return observers.Where(_ => _.EventTypes.Any(_ => eventTypeIdentifiers.Contains(_)));
    }
}
