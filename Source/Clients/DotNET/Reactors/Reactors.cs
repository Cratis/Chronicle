// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Traces;
using Grpc.Core;
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
    readonly IClientArtifactsActivator _artifactActivator;
    readonly IActivateReactorMiddlewares _middlewaresActivator;
    readonly IEventSerializer _eventSerializer;
    readonly ICausationManager _causationManager;
    readonly IIdentityProvider _identityProvider;
    readonly IActivitySource<Reactors> _activitySource;
    readonly IReactorSideEffectHandlers _sideEffectHandlers;
    readonly IReactorContextValuesBuilder _reactorContextValuesBuilder;
    readonly IReactorMethodArgumentsResolver _argumentsResolver;
    readonly ILogger<Reactors> _logger;
    readonly ILoggerFactory _loggerFactory;
    readonly IChronicleServicesAccessor _servicesAccessor;
    IReadOnlyDictionary<ReactorId, ReactorRegistration> _handlers = FrozenDictionary<ReactorId, ReactorRegistration>.Empty;

    bool _registered;

    /// <summary>
    /// Initializes a new instance of the <see cref="Reactors"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="IEventStore"/> the Reactors belong to.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for resolving event types.</param>
    /// <param name="clientArtifactsProvider"><see cref="IClientArtifactsProvider"/> for getting client artifacts.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> to get instances of types.</param>
    /// <param name="artifactActivator"><see cref="IClientArtifactsActivator"/> for creating artifact instances.</param>
    /// <param name="middlewaresActivator"><see cref="IReactorMiddlewares"/> to call.</param>
    /// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing of events.</param>
    /// <param name="causationManager"><see cref="ICausationManager"/> for working with causation.</param>
    /// <param name="identityProvider"><see cref="IIdentityProvider"/> for managing identity context.</param>
    /// <param name="activitySource"><see cref="IActivitySource{T}"/> for tracing reactor event handling.</param>
    /// <param name="sideEffectHandlers"><see cref="IReactorSideEffectHandlers"/> for processing return values as side effects.</param>
    /// <param name="reactorContextValuesBuilder"><see cref="IReactorContextValuesBuilder"/> for resolving append-metadata for side-effect events.</param>
    /// <param name="argumentsResolver"><see cref="IReactorMethodArgumentsResolver"/> for resolving handler method arguments.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public Reactors(
        IEventStore eventStore,
        IEventTypes eventTypes,
        IClientArtifactsProvider clientArtifactsProvider,
        IServiceProvider serviceProvider,
        IClientArtifactsActivator artifactActivator,
        IActivateReactorMiddlewares middlewaresActivator,
        IEventSerializer eventSerializer,
        ICausationManager causationManager,
        IIdentityProvider identityProvider,
        IActivitySource<Reactors> activitySource,
        IReactorSideEffectHandlers sideEffectHandlers,
        IReactorContextValuesBuilder reactorContextValuesBuilder,
        IReactorMethodArgumentsResolver argumentsResolver,
        ILogger<Reactors> logger,
        ILoggerFactory loggerFactory)
    {
        _eventStore = eventStore;
        _servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;
        _eventTypes = eventTypes;
        _clientArtifactsProvider = clientArtifactsProvider;
        _serviceProvider = serviceProvider;
        _artifactActivator = artifactActivator;
        _middlewaresActivator = middlewaresActivator;
        _eventSerializer = eventSerializer;
        _causationManager = causationManager;
        _identityProvider = identityProvider;
        _activitySource = activitySource;
        _sideEffectHandlers = sideEffectHandlers;
        _reactorContextValuesBuilder = reactorContextValuesBuilder;
        _argumentsResolver = argumentsResolver;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _eventStore.Connection.Lifecycle.OnDisconnected += () =>
        {
            lock (_registerLock)
            {
                _registered = false;
                RecreateHandlersForReconnect();
            }

            return Task.CompletedTask;
        };
    }

    /// <inheritdoc/>
    public Task Discover()
    {
        _logger.DiscoverAllReactors();
        lock (_registerLock)
        {
            var reactorTypes = _clientArtifactsProvider.Reactors.ToArray();
            var runtimeRegistrations = _handlers.Values.Where(_ => _.Handle is not null).ToArray();
            var ids = runtimeRegistrations.Select(_ => _.Handler.Id).Concat(reactorTypes.Select(_ => _.GetReactorId()));
            var duplicate = ids.GroupBy(_ => _).FirstOrDefault(_ => _.Count() > 1);
            if (duplicate is not null)
            {
                var collidingTypes = reactorTypes.Where(_ => _.GetReactorId() == duplicate.Key).Take(2).ToArray();
                if (collidingTypes.Length == 2)
                {
                    throw new ReactorAlreadyRegistered(duplicate.Key, collidingTypes[0], collidingTypes[1]);
                }

                throw new ReactorAlreadyRegistered(duplicate.Key);
            }

            var registrations = reactorTypes.Select(CreateRegistrationFor).ToArray();
            DisconnectHandlers();
            _registered = false;
            _handlers = registrations.Concat(runtimeRegistrations.Select(RecreateRegistration))
                .ToFrozenDictionary(_ => _.Handler.Id);
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

            foreach (var registration in _handlers.Values.Where(_ => !_.IsRegistered))
            {
                RegisterReactor(registration);
                registration.IsRegistered = true;
            }
            _registered = true;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReactorHandler> Register<TReactor>()
        where TReactor : IReactor
    {
        lock (_registerLock)
        {
            var registration = CreateRegistrationFor(typeof(TReactor));
            AddRegistration(registration);
            return Task.FromResult<IReactorHandler>(registration.Handler);
        }
    }

    /// <inheritdoc/>
    public Task<IReactorHandler> Register(ReactorId id, Action<IReactorDefinitionBuilder> configure, Func<ReactorEvent, CancellationToken, Task> handle)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(handle);
        var builder = new ReactorDefinitionBuilder();
        configure(builder);
        var definition = builder.Build(id);
        lock (_registerLock)
        {
#pragma warning disable CA2000 // Ownership of the handler transfers to the registration.
            var handler = CreateHandler(id, typeof(object), definition.EventSequenceId, definition.EventTypes);
#pragma warning restore CA2000
            var registration = new ReactorRegistration(
                handler,
                definition.IsReplayable,
                [],
                [],
                EventSourceType.Unspecified,
                EventStreamType.All,
                handle);
            AddRegistration(registration);
            return Task.FromResult<IReactorHandler>(handler);
        }
    }

    /// <inheritdoc/>
    public void Unregister(ReactorId id)
    {
        lock (_registerLock)
        {
            if (_handlers.TryGetValue(id, out var registration))
            {
                _handlers = _handlers.Where(_ => _.Key != id).ToFrozenDictionary();
                registration.Handler.Disconnect();
                (registration.Handler as IDisposable)?.Dispose();
            }
        }
    }

    /// <inheritdoc/>
    public IReactorHandler GetHandlerFor<TReactor>()
        where TReactor : IReactor => _handlers[typeof(TReactor).GetReactorId()].Handler;

    /// <inheritdoc/>
    public IReactorHandler GetHandlerById(ReactorId id)
    {
        if (_handlers.TryGetValue(id, out var registration))
        {
            return registration.Handler;
        }

        var request = new HasReactorRequest
        {
            EventStore = _eventStore.Name,
            Namespace = _eventStore.Namespace,
            ReactorId = id.Value
        };
        var response = _servicesAccessor.Services.Reactors.HasReactor(request).GetAwaiter().GetResult();
        if (!response.Exists)
        {
            ThrowIfUnknownReactorId(null, id);
        }

        return new ReactorHandler(
            _eventStore,
            id,
            typeof(object),
            new(response.EventSequenceId!),
            [],
            _causationManager,
            _identityProvider);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Observation.FailedPartition>> GetFailedPartitionsFor<TReactor>() =>
        GetFailedPartitionsFor(typeof(TReactor));

    /// <inheritdoc/>
    public Task<IEnumerable<Observation.FailedPartition>> GetFailedPartitionsFor(Type reactorType)
    {
        var handler = _handlers[reactorType.GetReactorId()].Handler;
        return handler.GetFailedPartitions();
    }

    /// <inheritdoc/>
    public Task<ReactorState> GetStateFor<TReactor>()
        where TReactor : IReactor
    {
        var reactorType = typeof(TReactor);
        var handler = _handlers[reactorType.GetReactorId()].Handler;
        return handler.GetState();
    }

    /// <inheritdoc/>
    public Task<JobId> Replay<TReactor>()
        where TReactor : IReactor
    {
        var reactorType = typeof(TReactor);
        var handler = _handlers[reactorType.GetReactorId()].Handler;
        return Replay(handler.Id);
    }

    /// <inheritdoc/>
    public async Task<JobId> Replay(ReactorId reactorId)
    {
        var response = await _servicesAccessor.Services.Observers.Replay(new Replay
        {
            EventStore = _eventStore.Name,
            Namespace = _eventStore.Namespace,
            ObserverId = reactorId,
            EventSequenceId = string.Empty
        });
        return Guid.TryParse(response.JobId, out var value) ? new JobId(value) : JobId.NotSet;
    }

    static void ThrowIfUnknownReactorId(IReactorHandler? handler, ReactorId reactorId)
    {
        if (handler is null)
        {
            throw new UnknownReactorId(reactorId);
        }
    }

    ReactorRegistration CreateRegistrationFor(Type reactorType)
    {
#pragma warning disable CA2000 // Ownership of the handler transfers to the registration.
        var handler = CreateHandler(
            reactorType.GetReactorId(),
            reactorType,
            reactorType.GetEventSequenceId(_eventStore.Name?.Value),
            ReactorInvoker.GetEventTypesFor(_eventStore.EventTypes, reactorType, _sideEffectHandlers));
#pragma warning restore CA2000
        return new(
            handler,
            !reactorType.IsDefined(typeof(OnceOnlyAttribute), inherit: false),
            [.. reactorType.GetTags()],
            [.. reactorType.GetFilterTags()],
            reactorType.GetEventSourceType(),
            reactorType.GetEventStreamType(),
            null);
    }

    ReactorHandler CreateHandler(ReactorId id, Type reactorType, EventSequenceId sequenceId, IEnumerable<EventType> eventTypes)
    {
        var handler = new ReactorHandler(_eventStore, id, reactorType, sequenceId, eventTypes, _causationManager, _identityProvider);
        handler.CancellationToken.Register(() =>
        {
            lock (_registerLock)
            {
                if (_handlers.TryGetValue(id, out var current) && ReferenceEquals(current.Handler, handler))
                {
                    _handlers = _handlers.Where(_ => _.Key != id).ToFrozenDictionary();
                }
            }
        });
        return handler;
    }

    void AddRegistration(ReactorRegistration registration)
    {
        if (_handlers.ContainsKey(registration.Handler.Id))
        {
            registration.Handler.Disconnect();
            (registration.Handler as IDisposable)?.Dispose();
            throw new ReactorAlreadyRegistered(registration.Handler.Id);
        }

        try
        {
            RegisterReactor(registration);
            registration.IsRegistered = true;
            _handlers = _handlers.Append(new KeyValuePair<ReactorId, ReactorRegistration>(registration.Handler.Id, registration))
                .ToFrozenDictionary();
        }
        catch
        {
            registration.Handler.Disconnect();
            registration.Handler.Dispose();
            throw;
        }
    }

    void DisconnectHandlers()
    {
        foreach (var registration in _handlers.Values.ToList())
        {
            registration.Handler.Disconnect();
            (registration.Handler as IDisposable)?.Dispose();
        }
    }

    void RecreateHandlersForReconnect()
    {
        var registrations = _handlers.Values.ToArray();
        DisconnectHandlers();
        _handlers = registrations.Select(RecreateRegistration).ToFrozenDictionary(_ => _.Handler.Id);
    }

    ReactorRegistration RecreateRegistration(ReactorRegistration registration)
    {
        var handler = registration.Handler;
        return registration with
        {
            Handler = CreateHandler(handler.Id, handler.ReactorType, handler.EventSequenceId, handler.EventTypes),
            IsRegistered = false
        };
    }

    void RegisterReactor(ReactorRegistration registration)
    {
        var handler = registration.Handler;
        _logger.RegisteringReactor(handler.Id);
        var request = new RegisterReactor
        {
            ConnectionId = _eventStore.Connection.Lifecycle.ConnectionId,
            EventStore = _eventStore.Name,
            Namespace = _eventStore.Namespace,
            Reactor = new ReactorDefinition
            {
                ReactorId = handler.Id,
                EventSequenceId = handler.EventSequenceId,
                EventTypes = handler.EventTypes.Select(et => new EventTypeWithKeyExpression { EventType = et.ToContract(), Key = WellKnownExpressions.EventSourceId }).ToArray(),
                IsReplayable = registration.IsReplayable,
                Tags = registration.Tags,
                Filters = new()
                {
                    FilterTags = registration.FilterTags,
                    EventSourceType = registration.EventSourceType.Value,
                    EventStreamType = registration.EventStreamType.Value
                }
            }
        };

#pragma warning disable CA2000 // Dispose objects before losing scope
        var messages = new BehaviorSubject<ReactorMessage>(new(new(request)));
#pragma warning restore CA2000 // Dispose objects before losing scope
        var cancellationToken = handler.CancellationToken;
        var eventsToObserve = _servicesAccessor.Services.Reactors.Observe(messages, cancellationToken);

        // Re-establish the observation after the stream ends. A cross-store
        // (inbox) reactor's stream can be CLOSED by the kernel rather than tailed
        // forever, so onCompleted fires with no error — without re-subscribing
        // there, the reactor silently goes Disconnected and never recovers until
        // the whole client reconnects (which is why cross-service invite accepts
        // stranded). The 2s delay avoids a hot loop if the stream keeps ending.
        void ScheduleReconnect()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    _logger.ReconnectingReactor(handler.Id);
                    RegisterReactor(registration);
                }
                catch (OperationCanceledException ex)
                {
                    _logger.RegisteringReactorStreamCancelled(handler.Id, ex);
                }
            });
        }

        // https://github.com/dotnet/reactive/issues/459
        eventsToObserve
            .Select(events => Observable.FromAsync(async () =>
            {
                await ObserverMethod(messages, registration, events, cancellationToken);
                _logger.EventHandlingCompleted(handler.Id);
            }))
            .Concat()
            .Subscribe(
                _ => { },
                ex =>
                {
                    if (IsExpectedCancellation(ex, cancellationToken))
                    {
                        _logger.RegisteringReactorStreamCancelled(handler.Id, ex);
                        messages.Dispose();
                        return;
                    }

                    var streamFailed = new ReactorObservationStreamFailed(handler.Id, ex);
                    _logger.RegisteringReactorFailed(handler.Id, streamFailed);
                    messages.Dispose();
                    ScheduleReconnect();
                },
                () =>
                {
                    _logger.RegisteringReactorStreamCompleted(handler.Id);
                    messages.Dispose();
                    ScheduleReconnect();
                });
    }

    bool IsExpectedCancellation(Exception exception, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return true;
        }

        if (exception is OperationCanceledException)
        {
            return true;
        }

        if (exception is RpcException rpcException && rpcException.StatusCode == StatusCode.Cancelled)
        {
            return true;
        }

        if (exception.InnerException is not null)
        {
            return IsExpectedCancellation(exception.InnerException, cancellationToken);
        }

        return false;
    }

    async Task ObserverMethod(BehaviorSubject<ReactorMessage> messages, ReactorRegistration registration, EventsToObserve events, CancellationToken cancellationToken)
    {
        var handler = registration.Handler;
        if (events.ReplayState != ReplayState.None)
        {
            if (registration.Handle is null)
            {
                await HandleReplayNotification(handler, events.ReplayState, events.Partition);
            }
            return;
        }

        using var span = _activitySource.Handle(
            _eventStore.Name,
            _eventStore.Namespace,
            handler.EventSequenceId,
            handler.Id);

        var lastSuccessfullyObservedEvent = EventSequenceNumber.Unavailable;
        var exceptionMessages = Enumerable.Empty<string>();
        var exceptionStackTrace = string.Empty;
        var state = ObservationState.Success;

        if (registration.Handle is not null)
        {
            foreach (var @event in events.Events)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                _logger.EventReceived(@event.Context.EventType.Id, handler.Id);
                try
                {
                    var delivered = new ReactorEvent(
                        @event.Context.ToClient(),
                        JsonNode.Parse(@event.Content)!.AsObject(),
                        new Dictionary<int, string>(@event.GenerationalContent));
                    using (handler.BeginHandlingScope(delivered.Context))
                    {
                        try
                        {
                            await registration.Handle(delivered, cancellationToken);
                        }
                        finally
                        {
                            handler.EndHandling();
                        }
                    }
                    lastSuccessfullyObservedEvent = @event.Context.SequenceNumber;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    FailedToHandleEventWithException(ex, @event.Context.EventType.Id);
                    break;
                }
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                PublishResult();
            }
            return;
        }

        await using var serviceProviderScope = _serviceProvider.CreateAsyncScope();
        var activatedReactorResult = _artifactActivator.Activate(serviceProviderScope.ServiceProvider, handler.ReactorType);
        if (activatedReactorResult.TryGetException(out var exception))
        {
            FailedActivatingReactor(exception);
            PublishResult();
            return;
        }

        await using var activatedReactor = activatedReactorResult.AsT0;
        await using var middlewares = _middlewaresActivator.Activate(serviceProviderScope.ServiceProvider);
        var reactorInvoker = new ReactorInvoker(
            _eventTypes,
            middlewares,
            handler.ReactorType,
            activatedReactor,
            _loggerFactory.CreateLogger<ReactorInvoker>(),
            _sideEffectHandlers,
            _eventStore,
            _reactorContextValuesBuilder,
            _argumentsResolver,
            serviceProviderScope.ServiceProvider);

        foreach (var @event in events.Events)
        {
            _logger.EventReceived(@event.Context.EventType.Id, handler.Id);

            try
            {
                var context = @event.Context.ToClient();

                var handlerEventType = handler.EventTypes.FirstOrDefault(et => et.Id == context.EventType.Id);
                var targetGeneration = handlerEventType?.Generation ?? context.EventType.Generation;
                var eventType = _eventTypes.GetClrTypeFor(context.EventType.Id, targetGeneration);

                string contentJson;
                if (targetGeneration != context.EventType.Generation &&
                    @event.GenerationalContent.TryGetValue((int)targetGeneration.Value, out var genContent))
                {
                    contentJson = genContent;
                    context = context with { EventType = context.EventType with { Generation = targetGeneration } };
                }
                else
                {
                    contentJson = @event.Content;
                }

                var content = await _eventSerializer.Deserialize(eventType, JsonNode.Parse(contentJson)!.AsObject());

                var handleResult = await handler.OnNext(context, content, reactorInvoker);
                if (handleResult.IsFailed)
                {
                    FailedToHandleEvent(handleResult, @event.Context.EventType.Id);
                    break;
                }
                lastSuccessfullyObservedEvent = @event.Context.SequenceNumber;
            }
            catch (Exception ex)
            {
                FailedToHandleEventWithException(ex, @event.Context.EventType.Id);
                break;
            }
        }

        PublishResult();

        void FailedToHandleEvent(ReactorInvocationResult invocationResult, EventTypeId eventTypeId)
        {
            var failureDetails = invocationResult.GetFailureDetails();
            exceptionMessages = failureDetails.Messages.ToArray();
            exceptionStackTrace = failureDetails.StackTrace;
            state = ObservationState.Failed;

            if (invocationResult.ExceptionResult.TryGetException(out var ex))
            {
                _logger.ErrorWhileHandlingEvent(ex, eventTypeId, handler.Id);
            }
            else
            {
                var targetEventSourceIds = invocationResult.SideEffectFailure is not null
                    ? string.Join(", ", invocationResult.SideEffectFailure.GetTargetEventSourceIds())
                    : string.Empty;
                _logger.ReactorSideEffectAppendFailed(eventTypeId, handler.Id, targetEventSourceIds, string.Join(Environment.NewLine, exceptionMessages));
            }
        }

        void FailedToHandleEventWithException(Exception ex, EventTypeId eventTypeId)
        {
            _logger.ErrorWhileHandlingEvent(ex, eventTypeId, handler.Id);
            exceptionMessages = ex.GetAllMessages();
            exceptionStackTrace = ex.StackTrace ?? string.Empty;
            state = ObservationState.Failed;
        }

        void FailedActivatingReactor(Exception ex)
        {
            exceptionMessages = ex.GetAllMessages();
            exceptionStackTrace = ex.StackTrace ?? string.Empty;
            state = ObservationState.Failed;
        }

        void PublishResult()
        {
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

    async Task HandleReplayNotification(ReactorHandler handler, ReplayState replayState, string partition)
    {
        await using var serviceProviderScope = _serviceProvider.CreateAsyncScope();
        var activatedReactorResult = _artifactActivator.Activate(serviceProviderScope.ServiceProvider, handler.ReactorType);
        if (activatedReactorResult.TryGetException(out var ex))
        {
            _logger.FailedActivatingReactorForReplayNotification(ex, handler.Id, replayState);
            return;
        }

        await using var activatedReactor = activatedReactorResult.AsT0;
        switch (replayState)
        {
            case ReplayState.BeginReplay when activatedReactor.Instance is ICanBeNotifiedWhenReplay notifiable:
                await notifiable.BeginReplay();
                break;

            case ReplayState.EndReplay when activatedReactor.Instance is ICanBeNotifiedWhenReplay notifiable:
                await notifiable.EndReplay();
                break;

            case ReplayState.BeginReplayPartition when activatedReactor.Instance is ICanBeNotifiedWhenPartitionReplayed notifiable:
                await notifiable.BeginReplayPartition(partition);
                break;

            case ReplayState.EndReplayPartition when activatedReactor.Instance is ICanBeNotifiedWhenPartitionReplayed notifiable:
                await notifiable.EndReplayPartition(partition);
                break;
        }
    }

    sealed record ReactorRegistration(
        ReactorHandler Handler,
        bool IsReplayable,
        string[] Tags,
        string[] FilterTags,
        EventSourceType EventSourceType,
        EventStreamType EventStreamType,
        Func<ReactorEvent, CancellationToken, Task>? Handle)
    {
        /// <summary>
        /// Gets or sets whether the subscription has been sent on the current connection.
        /// </summary>
        public bool IsRegistered { get; set; }
    }
}
