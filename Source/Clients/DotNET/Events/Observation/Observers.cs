// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Connections;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Events.Store.Grains.Observation;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Types;
using Orleans;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Observation;

/// <summary>
/// Represents an implementation of <see cref="Observers"/>.
/// </summary>
public class Observers : IObservers
{
    readonly IEnumerable<ObserverHandler> _observerHandlers;
    readonly IClusterClient _clusterClient;
    readonly IConnectionManager _connectionManager;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of <see cref="Observers"/>.
    /// </summary>
    /// <param name="clusterClient"><see cref="IClusterClient"/> for working with Orleans.</param>
    /// <param name="connectionManager"><see cref="IConnectionManager"/> for getting current connection information.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for establishing execution context.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> to get instances of types.</param>
    /// <param name="middlewares"><see cref="IObserverMiddlewares"/> to call.</param>
    /// <param name="eventTypes">Registered <see cref="IEventTypes"/>.</param>
    /// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing of events.</param>
    /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
    public Observers(
        IClusterClient clusterClient,
        IConnectionManager connectionManager,
        IExecutionContextManager executionContextManager,
        IServiceProvider serviceProvider,
        IObserverMiddlewares middlewares,
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        ITypes types)
    {
        _observerHandlers = types.AllObservers()
                            .Select(_ =>
                            {
                                var observer = _.GetCustomAttribute<ObserverAttribute>()!;
                                return new ObserverHandler(
                                    observer.ObserverId,
                                    _.FullName ?? $"{_.Namespace}.{_.Name}",
                                    observer.EventLogId,
                                    eventTypes,
                                    new ObserverInvoker(serviceProvider, eventTypes, middlewares, _),
                                    eventSerializer);
                            });
        _clusterClient = clusterClient;
        _connectionManager = connectionManager;
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public async Task StartObserving()
    {
        // TODO: Observe for all tenants
        _executionContextManager.Establish("3352d47d-c154-4457-b3fb-8a2efb725113", CorrelationId.New());
        var streamProvider = _clusterClient.GetStreamProvider("observer-handlers");

        foreach (var handler in _observerHandlers)
        {
            var stream = streamProvider.GetStream<AppendedEvent>(handler.ObserverId, _connectionManager.CurrentConnectionId.Value);
            var subscription = await stream.SubscribeAsync(async (@event, _) =>
            {
                // TODO: Establish in the correct context
                _executionContextManager.Establish("3352d47d-c154-4457-b3fb-8a2efb725113", CorrelationId.New());
                await handler.OnNext(@event);
            });

            var observer = _clusterClient.GetGrain<IObserver>(handler.ObserverId, keyExtension: handler.EventLogId.ToString());
            var eventTypes = handler.EventTypes.ToArray();
            await observer.Subscribe(eventTypes);
        }
    }
}
