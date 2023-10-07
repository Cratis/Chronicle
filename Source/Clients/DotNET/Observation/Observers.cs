// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Observation;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObservers"/>.
/// </summary>
public class Observers : IObservers
{
    readonly IEventStore _eventStore;
    readonly IClientArtifactsProvider _clientArtifactsProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observers"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="IEventStore"/> the observers belong to.</param>
    /// <param name="clientArtifactsProvider"><see cref="IClientArtifactsProvider"/> for getting client artifacts.</param>
    public Observers(
        IEventStore eventStore,
        IClientArtifactsProvider clientArtifactsProvider)
    {
        _eventStore = eventStore;
        eventStore.Connection.Lifecycle.OnConnected += () =>
        {
            Console.WriteLine("Hello world");
            return Task.CompletedTask;
        };
        _clientArtifactsProvider = clientArtifactsProvider;
    }

    /// <inheritdoc/>
    public Task RegisterKnownObservers()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ObserverInformation>> GetAllObservers()
    {
        // var tenantId = _executionContextManager.Current.TenantId;
        // var microserviceId = _executionContextManager.Current.MicroserviceId;
        // var route = $"/api/events/store/{microserviceId}/{tenantId}/observers";
        // var result = await _connection.PerformQuery<IEnumerable<ObserverInformation>>(route);
        // return result.Data;
        await Task.CompletedTask;
        return null!;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ObserverInformation>> GetObserversForEventTypes(IEnumerable<Type> eventTypes)
    {
        var observers = await GetAllObservers();
        var eventTypeIdentifiers = eventTypes.Select(_ => _eventStore.EventTypes.GetEventTypeFor(_));
        return observers.Where(_ => _.EventTypes.Any(_ => eventTypeIdentifiers.Contains(_)));
    }
}
