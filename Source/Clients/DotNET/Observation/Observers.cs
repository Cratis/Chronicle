// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Types;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Represents an implementation of <see cref="Observers"/>.
/// </summary>
public class Observers : IObservers
{
    readonly IEnumerable<ObserverHandler> _observerHandlers;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of <see cref="Observers"/>.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for establishing execution context.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> to get instances of types.</param>
    /// <param name="middlewares"><see cref="IObserverMiddlewares"/> to call.</param>
    /// <param name="eventTypes">Registered <see cref="IEventTypes"/>.</param>
    /// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing of events.</param>
    /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
    public Observers(
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
                                    observer.EventSequenceId,
                                    eventTypes,
                                    new ObserverInvoker(serviceProvider, eventTypes, middlewares, _),
                                    eventSerializer);
                            });
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public Task RegisterAndObserveAll()
    {
        return Task.CompletedTask;
    }
}
