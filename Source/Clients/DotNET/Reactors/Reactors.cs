// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents an implementation of <see cref="IReactors"/>.
/// </summary>
public class Reactors : IReactors
{
#if NET8_0
    static readonly object _registerLock = new();
#else
    static readonly Lock _registerLock = new();
#endif
    readonly IEventStore _eventStore;
    readonly IEventTypes _eventTypes;
    readonly IClientArtifactsProvider _clientArtifactsProvider;
    readonly IServiceProvider _serviceProvider;
    readonly IReactorMiddlewares _middlewares;
    readonly IEventSerializer _eventSerializer;
    readonly ICausationManager _causationManager;
    readonly IIdentityProvider _identityProvider;
    readonly ILogger<Reactors> _logger;
    readonly ILoggerFactory _loggerFactory;
    readonly IDictionary<Type, IReactorHandler> _handlers = new Dictionary<Type, IReactorHandler>();
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
    /// <param name="identityProvider"><see cref="IIdentityProvider"/> for managing identity context.</param>
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
        IIdentityProvider identityProvider,
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
        _identityProvider = identityProvider;
        _logger = logger;
        _loggerFactory = loggerFactory;
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
    public Task<IReactorHandler> Register<TReactor>()
        where TReactor : IReactor
    {
        var reactorType = typeof(TReactor);
        var reactorHandler = CreateHandlerFor(reactorType);

        RegisterReactor(reactorHandler);
        _handlers.Add(reactorType, reactorHandler);

        return Task.FromResult(reactorHandler);
    }

    /// <inheritdoc/>
    public IReactorHandler GetHandlerFor<TReactor>()
        where TReactor : IReactor => _handlers[typeof(TReactor)];

    /// <inheritdoc/>
    public IReactorHandler GetHandlerById(ReactorId id)
    {
        var reactorHandler = _handlers.Values.SingleOrDefault(_ => _.Id == id);
        ThrowIfUnknownReactorId(reactorHandler, id);
        return reactorHandler!;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Observation.FailedPartition>> GetFailedPartitionsFor<TReactor>() =>
        GetFailedPartitionsFor(typeof(TReactor));

    /// <inheritdoc/>
    public Task<IEnumerable<Observation.FailedPartition>> GetFailedPartitionsFor(Type reactorType)
    {
        var handler = _handlers[reactorType];
        return handler.GetFailedPartitions();
    }

    /// <inheritdoc/>
    public Task<ReactorState> GetStateFor<TReactor>()
        where TReactor : IReactor
    {
        var reactorType = typeof(TReactor);
        var handler = _handlers[reactorType];
        return handler.GetState();
    }

    /// <inheritdoc/>
    public Task Replay<TReactor>()
        where TReactor : IReactor
    {
        var reactorType = typeof(TReactor);
        var handler = _handlers[reactorType];
        return Replay(handler.Id);
    }

    /// <inheritdoc/>
    public Task Replay(ReactorId reactorId)
    {
        return _servicesAccessor.Services.Observers.Replay(new Replay
        {
            EventStore = _eventStore.Name,
            Namespace = _eventStore.Namespace,
            ObserverId = reactorId,
            EventSequenceId = string.Empty
        });
    }

    static void ThrowIfUnknownReactorId(IReactorHandler? handler, ReactorId reactorId)
    {
        if (handler is null)
        {
            throw new UnknownReactorId(reactorId);
        }
    }

    IReactorHandler CreateHandlerFor(Type reactorType)
    {
        var handler = new ReactorHandler(
            _eventStore,
            reactorType.GetReactorId(),
            reactorType,
            reactorType.GetEventSequenceId(),
            new ReactorInvoker(_eventStore.EventTypes, _middlewares, reactorType, _loggerFactory.CreateLogger<ReactorInvoker>()),
            _causationManager,
            _identityProvider);

        CancellationTokenRegistration? register = null;
        register = handler.CancellationToken.Register(() =>
        {
            _handlers.Remove(reactorType);
            register?.Dispose();
        });
        return handler;
    }

    void RegisterReactor(IReactorHandler handler)
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
                EventTypes = handler.EventTypes.Select(et => new EventTypeWithKeyExpression { EventType = et.ToContract(), Key = WellKnownExpressions.EventSourceId }).ToArray(),
                Categories = handler.ReactorType.GetCategories().ToArray()
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

    async Task ObserverMethod(BehaviorSubject<ReactorMessage> messages, IReactorHandler handler, EventsToObserve events)
    {
        var lastSuccessfullyObservedEvent = EventSequenceNumber.Unavailable;
        var exceptionMessages = Enumerable.Empty<string>();
        var exceptionStackTrace = string.Empty;
        var state = ObservationState.Success;
        await using var serviceProviderScope = _serviceProvider.CreateAsyncScope();
        foreach (var @event in events.Events)
        {
            _logger.EventReceived(@event.Context.EventType.Id, handler.Id);

            try
            {
                var context = @event.Context.ToClient();

                var eventType = _eventTypes.GetClrTypeFor(context.EventType.Id);
                var content = await _eventSerializer.Deserialize(eventType, JsonNode.Parse(@event.Content)!.AsObject());

                await handler.OnNext(context, content, serviceProviderScope.ServiceProvider);
                lastSuccessfullyObservedEvent = @event.Context.SequenceNumber;
            }
            catch (Exception ex)
            {
                _logger.ErrorWhileHandlingEvent(ex, @event.Context.EventType.Id, handler.Id);
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
