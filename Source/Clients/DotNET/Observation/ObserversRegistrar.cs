// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Connections;
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
    readonly IConnection _connection;
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
    /// <param name="connection"><see cref="IConnection"/> for working with kernel.</param>
    /// <param name="causationManager"><see cref="ICausationManager"/> for working with causation.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ObserversRegistrar(
        IExecutionContextManager executionContextManager,
        IServiceProvider serviceProvider,
        IObserverMiddlewares middlewares,
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        IClientArtifactsProvider clientArtifacts,
        IConnection connection,
        ICausationManager causationManager,
        ILogger<ObserversRegistrar> logger)
    {
        _handlers = clientArtifacts.Observers
                            .ToDictionary(
                                _ => _,
                                observerType =>
                                {
                                    var observer = observerType.GetCustomAttribute<ObserverAttribute>()!;
                                    return new ObserverHandler(
                                        observer.ObserverId,
                                        observerType.FullName ?? $"{observerType.Namespace}.{observerType.Name}",
                                        observer.EventSequenceId,
                                        new ObserverInvoker(serviceProvider, eventTypes, middlewares, observerType),
                                        causationManager,
                                        eventSerializer);
                                });
        _executionContextManager = executionContextManager;
        _connection = connection;
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
        var route = $"/api/events/store/{microserviceId}/observers/register/{_connection.ConnectionId}";
        var registrations = _handlers.Values.Select(_ => new ClientObserverRegistration(
            _.ObserverId,
            _.Name,
            _.EventSequenceId,
            _.EventTypes)).ToArray();
        await _connection.PerformCommand(route, registrations);
    }

    void ThrowIfTypeIsNotAnObserver(Type observerType)
    {
        if (!_handlers.ContainsKey(observerType))
        {
            throw new UnknownObserverType(observerType);
        }
    }
}
