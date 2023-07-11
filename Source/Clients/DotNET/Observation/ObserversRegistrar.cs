// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Clients;
using Aksio.Cratis.Events;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Represents an implementation of <see cref="ObserversRegistrar"/>.
/// </summary>
[Singleton]
public class ObserversRegistrar : IObserversRegistrar
{
    readonly IExecutionContextManager _executionContextManager;
    readonly IClient _client;
    readonly ILogger<ObserversRegistrar> _logger;
    readonly IDictionary<Type, ObserverHandler> _handlers;

    /// <summary>
    /// Initializes a new instance of <see cref="ObserversRegistrar"/>.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for establishing execution context.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> to get instances of types.</param>
    /// <param name="middlewares"><see cref="IObserverMiddlewares"/> to call.</param>
    /// <param name="eventTypes">Registered <see cref="IEventTypes"/>.</param>
    /// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing of events.</param>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    /// <param name="client"><see cref="IClient"/> for working with kernel.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ObserversRegistrar(
        IExecutionContextManager executionContextManager,
        IServiceProvider serviceProvider,
        IObserverMiddlewares middlewares,
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        IClientArtifactsProvider clientArtifacts,
        IClient client,
        ILogger<ObserversRegistrar> logger)
    {
        _handlers = clientArtifacts.Observers
                            .ToDictionary(
                                _ => _,
                                _ =>
                                {
                                    var observer = _.GetCustomAttribute<ObserverAttribute>()!;
                                    return new ObserverHandler(
                                        observer.ObserverId,
                                        _.FullName ?? $"{_.Namespace}.{_.Name}",
                                        observer.EventSequenceId,
                                        eventTypes,
                                        new ObserverInvoker(serviceProvider, eventTypes, middlewares, _),
                                        eventSerializer);
                                });
        _executionContextManager = executionContextManager;
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IEnumerable<ObserverHandler> GetAll() => _handlers.Values;

    /// <inheritdoc/>
    public ObserverHandler GetById(ObserverId observerId)
    {
        var observer = _handlers.Values.SingleOrDefault(_ => _.ObserverId == observerId);
        ObserverDoesNotExist.ThrowIfDoesNotExist(observerId, observer);
        return observer!;
    }

    /// <inheritdoc/>
    public ObserverHandler GetByType(Type observerType)
    {
        ThrowIfTypeIsNotAnObserver(observerType);
        return _handlers[observerType];
    }

    /// <inheritdoc/>
    public Type GetClrType(ObserverId observerId)
    {
        var observer = _handlers.SingleOrDefault(_ => _.Value.ObserverId == observerId);
        ObserverDoesNotExist.ThrowIfDoesNotExist(observerId, observer.Value);
        return observer.Key;
    }

    /// <inheritdoc/>
    public async Task Initialize()
    {
        _logger.RegisterObservers();

        foreach (var observerHandler in _handlers.Values)
        {
            _logger.RegisterObserver(
                observerHandler.ObserverId,
                observerHandler.Name,
                observerHandler.EventSequenceId);
        }

        var microserviceId = _executionContextManager.Current.MicroserviceId;
        var route = $"/api/events/store/{microserviceId}/observers/register/{_client.ConnectionId}";
        var registrations = _handlers.Values.Select(_ => new ClientObserverRegistration(
            _.ObserverId,
            _.Name,
            _.EventSequenceId,
            _.EventTypes));
        await _client.PerformCommand(route, registrations);
    }

    void ThrowIfTypeIsNotAnObserver(Type observerType)
    {
        if (!_handlers.ContainsKey(observerType))
        {
            throw new TypeIsNotAnObserver(observerType);
        }
    }
}
