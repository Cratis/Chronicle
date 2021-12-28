// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Events.Store.Grains.Observation;
using Cratis.Execution;
using Cratis.Types;
using Orleans;

namespace Cratis.Events.Observation
{
    /// <summary>
    /// Represents an implementation of <see cref="Observers"/>.
    /// </summary>
    public class Observers : IObservers
    {
        readonly IEnumerable<ObserverHandler> _observerHandlers;
        readonly IClusterClient _clusterClient;
        readonly IExecutionContextManager _executionContextManager;

        /// <summary>
        /// Initializes a new instance of <see cref="Observers"/>.
        /// </summary>
        /// <param name="clusterClient"><see cref="IClusterClient"/> for working with Orleans.</param>
        /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for establishing execution context.</param>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/> to work with instances of <see cref="IObserverHandler"/> types.</param>
        /// <param name="eventTypes">Registered <see cref="IEventTypes"/>.</param>
        /// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing of events.</param>
        /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
        public Observers(
            IClusterClient clusterClient,
            IExecutionContextManager executionContextManager,
            IServiceProvider serviceProvider,
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
                                        new ObserverInvoker(serviceProvider, eventTypes, _),
                                        eventSerializer);
                                });
            _clusterClient = clusterClient;
            _executionContextManager = executionContextManager;
        }

        /// <inheritdoc/>
        public async Task StartObserving()
        {
            _executionContextManager.Establish("f455c031-630e-450d-a75b-ca050c441708", CorrelationId.New());

            foreach (var handler in _observerHandlers)
            {
                var observer = _clusterClient.GetGrain<IObserver>(handler.ObserverId, keyExtension: handler.EventLogId.ToString());
                var observerHandler = await _clusterClient.CreateObjectReference<IObserverHandler>(handler);
                await observer.Subscribe(Array.Empty<EventType>(), observerHandler);
            }
        }
    }
}
