// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Reflection;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Contracts.Observation;
using Microsoft.Extensions.Logging;
using ObserverInformation = Aksio.Cratis.Kernel.Observation.ObserverInformation;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObservers"/>.
/// </summary>
public class Observers : IObservers
{
    readonly IEventStore _eventStore;
    readonly IClientArtifactsProvider _clientArtifactsProvider;
    readonly IServiceProvider _serviceProvider;
    readonly IObserverMiddlewares _middlewares;
    readonly IEventSerializer _eventSerializer;
    readonly ICausationManager _causationManager;
    readonly ILoggerFactory _loggerFactory;
    readonly IDictionary<Type, ObserverHandler> _handlers = new Dictionary<Type, ObserverHandler>();

    /// <summary>
    /// Initializes a new instance of the <see cref="Observers"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="IEventStore"/> the observers belong to.</param>
    /// <param name="clientArtifactsProvider"><see cref="IClientArtifactsProvider"/> for getting client artifacts.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> to get instances of types.</param>
    /// <param name="middlewares"><see cref="IObserverMiddlewares"/> to call.</param>
    /// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing of events.</param>
    /// <param name="causationManager"><see cref="ICausationManager"/> for working with causation.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public Observers(
        IEventStore eventStore,
        IClientArtifactsProvider clientArtifactsProvider,
        IServiceProvider serviceProvider,
        IObserverMiddlewares middlewares,
        IEventSerializer eventSerializer,
        ICausationManager causationManager,
        ILoggerFactory loggerFactory)
    {
        _eventStore = eventStore;
        _clientArtifactsProvider = clientArtifactsProvider;
        _serviceProvider = serviceProvider;
        _middlewares = middlewares;
        _eventSerializer = eventSerializer;
        _causationManager = causationManager;
        _loggerFactory = loggerFactory;
        eventStore.Connection.Lifecycle.OnConnected += Register;
    }

    /// <inheritdoc/>
    public Task Discover()
    {
        var logger = _loggerFactory.CreateLogger<ObserverInvoker>();
        var handlers = _clientArtifactsProvider.Observers
                            .ToDictionary(
                                _ => _,
                                observerType =>
                                {
                                    var observer = observerType.GetCustomAttribute<ObserverAttribute>()!;
                                    return new ObserverHandler(
                                        observer.ObserverId,
                                        observerType.FullName ?? $"{observerType.Namespace}.{observerType.Name}",
                                        observer.EventSequenceId,
                                        _eventStore.EventTypes,
                                        new ObserverInvoker(_serviceProvider, _eventStore.EventTypes, _middlewares, observerType, logger),
                                        _causationManager,
                                        _eventSerializer);
                                });

        foreach (var handler in handlers)
        {
            _handlers.Add(handler);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Register()
    {
        foreach (var handler in _handlers.Values)
        {
            RegisterObserver(handler);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public ObserverHandler GetHandlerById(ObserverId id)
    {
        var observerHandler = _handlers.Values.SingleOrDefault(_ => _.ObserverId == id);
        ThrowIfUnknownObserverId(observerHandler, id);
        return observerHandler!;
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

    void ThrowIfUnknownObserverId(ObserverHandler? handler, ObserverId observerId)
    {
        if (handler is null)
        {
            throw new UnknownObserverId(observerId);
        }
    }

    void RegisterObserver(ObserverHandler handler)
    {
        var registration = new RegisterObserver
        {
            ConnectionId = _eventStore.Connection.Lifecycle.ConnectionId,
            EventStoreName = _eventStore.EventStoreName,
            TenantId = _eventStore.TenantId,
            EventSequenceId = handler.EventSequenceId.ToString(),
            ObserverId = handler.ObserverId.ToString(),
            ObserverName = handler.Name,
            EventTypes = handler.EventTypes.Select(_ => _.ToContract()).ToArray()
        };
        var messages = new BehaviorSubject<ObserverClientMessage>(new(new(registration)));

        Console.WriteLine($"Hello - {registration.ObserverId}");
        var eventsToObserve = _eventStore.Connection.Services.Observers.Observe(messages);
        eventsToObserve.Subscribe(async events =>
        {
            Console.WriteLine("hello");
            var result = new ObservationResult
            {
                State = ObservationState.Success
            };
            messages.OnNext(new(new(result)));
            await Task.CompletedTask;
        });
    }
}
