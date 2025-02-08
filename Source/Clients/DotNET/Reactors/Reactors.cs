// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents an implementation of <see cref="IReactors"/>.
/// </summary>
public class Reactors : IReactors
{
#if NET9_0
    static readonly Lock _registerLock = new();
#endif
#if NET8_0
    static readonly object _registerLock = new();
#endif
    readonly IEventStore _eventStore;
    readonly IEventTypes _eventTypes;
    readonly IClientArtifactsProvider _clientArtifactsProvider;
    readonly IServiceProvider _serviceProvider;
    readonly IReactorMiddlewares _middlewares;
    readonly IEventSerializer _eventSerializer;
    readonly ICausationManager _causationManager;
    readonly ILogger<Reactors> _logger;
    readonly ILoggerFactory _loggerFactory;
    readonly IDictionary<Type, ReactorHandler> _handlers = new Dictionary<Type, ReactorHandler>();
    readonly IChronicleServicesAccessor _servicesAccessor;

    bool _registered;

    /// <summary>
    /// Initializes a new instance of the <see cref="Reactors"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="IEventStore"/> the Reactors belong to.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for resolving event types.</param>
    /// <param name="clientArtifactsProvider"><see cref="IClientArtifactsProvider"/> for getting client artifacts.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> to get instances of types.</param>
    /// <param name="middlewares"><see cref="IReactorMiddlewares"/> to call.</param>
    /// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing of events.</param>
    /// <param name="causationManager"><see cref="ICausationManager"/> for working with causation.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public Reactors(
        IEventStore eventStore,
        IEventTypes eventTypes,
        IClientArtifactsProvider clientArtifactsProvider,
        IServiceProvider serviceProvider,
        IReactorMiddlewares middlewares,
        IEventSerializer eventSerializer,
        ICausationManager causationManager,
        ILogger<Reactors> logger,
        ILoggerFactory loggerFactory)
    {
        _eventStore = eventStore;
        _servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;
        _eventTypes = eventTypes;
        _clientArtifactsProvider = clientArtifactsProvider;
        _serviceProvider = serviceProvider;
        _middlewares = middlewares;
        _eventSerializer = eventSerializer;
        _causationManager = causationManager;
        _logger = logger;
        _loggerFactory = loggerFactory;
        eventStore.Connection.Lifecycle.OnConnected += Register;
    }

    /// <inheritdoc/>
    public Task Discover()
    {
        _logger.DiscoverAllReactors();
        var logger = _loggerFactory.CreateLogger<ReactorInvoker>();
        var handlers = _clientArtifactsProvider.Reactors
                            .ToDictionary(
                                _ => _,
                                CreateHandlerFor);

        foreach (var handler in handlers)
        {
            _handlers.Add(handler);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Register()
    {
        if (_registered)
        {
            return Task.CompletedTask;
        }

        lock (_registerLock)
        {
            if (_registered)
            {
                return Task.CompletedTask;
            }

            foreach (var handler in _handlers.Values)
            {
                RegisterReactor(handler);
            }
            _registered = true;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<ReactorHandler> Register<TReactor>()
        where TReactor : IReactor
    {
        var reactorType = typeof(TReactor);
        var reactorHandler = CreateHandlerFor(reactorType);

        RegisterReactor(reactorHandler);
        _handlers.Add(reactorType, reactorHandler);

        return Task.FromResult(reactorHandler);
    }

    /// <inheritdoc/>
    public ReactorHandler GetHandlerById(ReactorId id)
    {
        var reactorHandler = _handlers.Values.SingleOrDefault(_ => _.Id == id);
        ThrowIfUnknownReactorId(reactorHandler, id);
        return reactorHandler!;
    }

    static void ThrowIfUnknownReactorId(ReactorHandler? handler, ReactorId reactorId)
    {
        if (handler is null)
        {
            throw new UnknownReactorId(reactorId);
        }
    }

    ReactorHandler CreateHandlerFor(Type reactorType)
    {
        var handler = new ReactorHandler(
                                    reactorType.GetReactorId(),
                                    reactorType.GetEventSequenceId(),
                                    new ReactorInvoker(_eventStore.EventTypes, _middlewares, reactorType, _loggerFactory.CreateLogger<ReactorInvoker>()),
                                    _causationManager);

        CancellationTokenRegistration? register = null;
        register = handler.CancellationToken.Register(() =>
        {
            _handlers.Remove(reactorType);
            register?.Dispose();
        });
        return handler;
    }

    void RegisterReactor(ReactorHandler handler)
    {
        _logger.RegisteringReactor(handler.Id);
        var registration = new RegisterReactor
        {
            ConnectionId = _eventStore.Connection.Lifecycle.ConnectionId,
            EventStore = _eventStore.Name,
            Namespace = _eventStore.Namespace,
            Reactor = new ReactorDefinition
            {
                ReactorId = handler.Id,
                EventSequenceId = handler.EventSequenceId,
                EventTypes = handler.EventTypes.Select(et => new EventTypeWithKeyExpression { EventType = et.ToContract(), Key = "$eventSourceId" }).ToArray()
            }
        };

#pragma warning disable CA2000 // Dispose objects before losing scope
        var messages = new BehaviorSubject<ReactorMessage>(new(new(registration)));
#pragma warning restore CA2000 // Dispose objects before losing scope
        var eventsToObserve = _servicesAccessor.Services.Reactors.Observe(messages, handler.CancellationToken);

        // https://github.com/dotnet/reactive/issues/459
        eventsToObserve
            .Select(events => Observable.FromAsync(async () =>
            {
                await ObserverMethod(messages, handler, events);
                _logger.EventHandlingCompleted(handler.Id);
            }))
            .Concat()
            .Subscribe(_ => { }, messages.Dispose);
    }

    async Task ObserverMethod(BehaviorSubject<ReactorMessage> messages, ReactorHandler handler, EventsToObserve events)
    {
        var lastSuccessfullyObservedEvent = EventSequenceNumber.Unavailable;
        var exceptionMessages = Enumerable.Empty<string>();
        var exceptionStackTrace = string.Empty;
        var state = ObservationState.Success;
        await using var serviceProviderScope = _serviceProvider.CreateAsyncScope();
        foreach (var @event in events.Events)
        {
            _logger.EventReceived(@event.Metadata.Type.Id, handler.Id);

            try
            {
                var context = @event.Context.ToClient();
                var metadata = @event.Metadata.ToClient();

                var eventType = _eventTypes.GetClrTypeFor(metadata.Type.Id);
                var content = await _eventSerializer.Deserialize(eventType, JsonNode.Parse(@event.Content)!.AsObject());

                await handler.OnNext(metadata, context, content, serviceProviderScope.ServiceProvider);
                lastSuccessfullyObservedEvent = @event.Metadata.SequenceNumber;
            }
            catch (Exception ex)
            {
                _logger.ErrorWhileHandlingEvent(ex, @event.Metadata.Type.Id, handler.Id);
                exceptionMessages = ex.GetAllMessages();
                exceptionStackTrace = ex.StackTrace ?? string.Empty;
                state = ObservationState.Failed;
                break;
            }
        }

        var result = new ReactorResult
        {
            Partition = events.Partition,
            State = state,
            LastSuccessfulObservation = lastSuccessfullyObservedEvent,
            ExceptionMessages = exceptionMessages.ToList(),
            ExceptionStackTrace = exceptionStackTrace
        };
        messages.OnNext(new(new(result)));
    }
}
