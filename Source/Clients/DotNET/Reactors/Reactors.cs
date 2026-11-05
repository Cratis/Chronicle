// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.ExceptionServices;
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
using Cratis.Chronicle.Serverless;
using Cratis.Traces;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents an implementation of <see cref="IReactors"/>.
/// </summary>
public partial class Reactors : IReactors, IReactorPartitionRecovery
{
#if NET8_0
    static readonly object _registerLock = new();
#else
    static readonly Lock _registerLock = new();
#endif
    readonly IEventStore _eventStore;
    readonly IEventTypes _eventTypes;
    readonly IClientArtifactsProvider _clientArtifactsProvider;
    readonly IEventSerializer _eventSerializer;
    readonly ICausationManager _causationManager;
    readonly IIdentityProvider _identityProvider;
    readonly IReactorSideEffectHandlers _sideEffectHandlers;
    readonly ILogger<Reactors> _logger;
    readonly ReactorDispatcher _dispatcher;
    readonly IChronicleServicesAccessor _servicesAccessor;
    readonly List<(ReactorId Id, Action<IReactorBuilder> Define)> _pendingDefinitions = [];
    IReadOnlyDictionary<ReactorId, ReactorRegistration> _handlers = FrozenDictionary<ReactorId, ReactorRegistration>.Empty;

    long _connectionGeneration;
    bool _registered;
    bool _released;

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
        : this(eventStore, eventTypes, clientArtifactsProvider, serviceProvider, artifactActivator, middlewaresActivator, eventSerializer, causationManager, identityProvider, activitySource, sideEffectHandlers, reactorContextValuesBuilder, argumentsResolver, logger, loggerFactory, ClientSpanNames.Legacy)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Reactors"/> class with the selected client span names.
    /// </summary>
    /// <param name="eventStore">The <see cref="IEventStore"/> the reactors belong to.</param>
    /// <param name="eventTypes">The <see cref="IEventTypes"/>.</param>
    /// <param name="clientArtifactsProvider">The <see cref="IClientArtifactsProvider"/>.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    /// <param name="artifactActivator">The <see cref="IClientArtifactsActivator"/>.</param>
    /// <param name="middlewaresActivator">The <see cref="IActivateReactorMiddlewares"/>.</param>
    /// <param name="eventSerializer">The <see cref="IEventSerializer"/>.</param>
    /// <param name="causationManager">The <see cref="ICausationManager"/>.</param>
    /// <param name="identityProvider">The <see cref="IIdentityProvider"/>.</param>
    /// <param name="activitySource">The <see cref="IActivitySource{T}"/> for tracing.</param>
    /// <param name="sideEffectHandlers">The <see cref="IReactorSideEffectHandlers"/>.</param>
    /// <param name="reactorContextValuesBuilder">The <see cref="IReactorContextValuesBuilder"/>.</param>
    /// <param name="argumentsResolver">The <see cref="IReactorMethodArgumentsResolver"/>.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="spanNames">The <see cref="ClientSpanNames"/> to use for client spans.</param>
    internal Reactors(
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
        ILoggerFactory loggerFactory,
        ClientSpanNames spanNames)
    {
        _eventStore = eventStore;
        _servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;
        _eventTypes = eventTypes;
        _clientArtifactsProvider = clientArtifactsProvider;
        _eventSerializer = eventSerializer;
        _causationManager = causationManager;
        _identityProvider = identityProvider;
        _sideEffectHandlers = sideEffectHandlers;
        _logger = logger;
        _dispatcher = new ReactorDispatcher(
            eventStore,
            eventTypes,
            serviceProvider,
            artifactActivator,
            middlewaresActivator,
            eventSerializer,
            activitySource,
            sideEffectHandlers,
            reactorContextValuesBuilder,
            argumentsResolver,
            logger,
            loggerFactory)
        {
            SpanNames = spanNames
        };
        _eventStore.Connection.Lifecycle.OnDisconnected += ConnectionDisconnected;
    }

    /// <summary>
    /// Gets the generation delivery policy supplied by the event store.
    /// </summary>
    internal Observation.EventGenerationDelivery GenerationDelivery { get; init; }

    /// <summary>
    /// Gets how the events of reactors are delivered, when supplied by the event store.
    /// </summary>
    internal ObserverDeliveries? Deliveries { get; init; }

    /// <summary>
    /// Gets how many partitions of one reactor are handled at the same time.
    /// </summary>
    internal int MaxConcurrentPartitions { get; init; } = PartitionedDeliveryScheduler.DefaultMaxConcurrentPartitions;

    /// <summary>
    /// Gets the delay before a reactor re-registers after its observation stream ended.
    /// </summary>
    internal TimeSpan ReconnectDelay { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets how the re-registration work is started. Specs replace this to run the work deterministically.
    /// </summary>
    internal Func<Func<Task>, Task> ScheduleReconnectWork { get; init; } = work => Task.Run(work);

    /// <summary>
    /// Gets the current discovered and runtime reactor handlers.
    /// </summary>
    internal IEnumerable<IReactorHandler> Handlers => _handlers.Values.Select(_ => _.Handler);

    /// <inheritdoc/>
    public Task Discover()
    {
        _logger.DiscoverAllReactors();
        lock (_registerLock)
        {
            var reactorTypes = _clientArtifactsProvider.Reactors.ToArray();
            var runtimeRegistrations = _handlers.Values.Where(_ => _.Handle is not null).ToArray();
            var pendingDefinitions = _pendingDefinitions.Select(_ => Define(_.Id, _.Define)).ToArray();
            var ids = runtimeRegistrations.Select(_ => _.Handler.Id)
                .Concat(pendingDefinitions.Select(_ => _.Id))
                .Concat(reactorTypes.Select(ResolveId));
            var duplicate = ids.GroupBy(_ => _).FirstOrDefault(_ => _.Count() > 1);
            if (duplicate is not null)
            {
                var collidingTypes = reactorTypes.Where(_ => ResolveId(_) == duplicate.Key).Take(2).ToArray();
                if (collidingTypes.Length == 2)
                {
                    throw new ReactorAlreadyRegistered(duplicate.Key, collidingTypes[0], collidingTypes[1]);
                }

                throw new ReactorAlreadyRegistered(duplicate.Key);
            }

            var registrations = reactorTypes.Select(CreateDiscoveredRegistration).ToArray();
            DisconnectHandlers();
            _registered = false;
            _handlers = registrations
                .Concat(runtimeRegistrations.Select(RecreateRegistration))
                .Concat(pendingDefinitions.Select(CreateRegistrationFor))
                .ToFrozenDictionary(_ => _.Handler.Id);

            // From here on they are runtime registrations like any other, carried across later discoveries and
            // reconnects by the registrations themselves.
            _pendingDefinitions.Clear();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Register()
    {
        if (_registered || _released)
        {
            return Task.CompletedTask;
        }

        WithCurrentServices(services =>
        {
            if (_registered || _released)
            {
                return;
            }

            foreach (var registration in _handlers.Values.Where(_ => !_.IsRegistered))
            {
                RegisterReactor(registration, services);
                registration.IsRegistered = true;
                registration.RegisterOnReconnect = false;
            }
            _registered = true;
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReactorHandler> Register<TReactor>()
        where TReactor : IReactor => AddRegistration(() => CreateRegistrationFor(typeof(TReactor)));

    /// <inheritdoc/>
    public Task<IReactorHandler> Register(ReactorId id, Action<IReactorDefinitionBuilder> configure, Func<ReactorEvent, CancellationToken, Task> handle)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(handle);
        var builder = new ReactorDefinitionBuilder();
        configure(builder);
        var definition = builder.Build(id);
        return AddRegistration(() =>
        {
#pragma warning disable CA2000 // Ownership of the handler transfers to the registration.
            var handler = CreateHandler(id, typeof(object), definition.EventSequenceId, definition.EventTypes);
#pragma warning restore CA2000
            return new ReactorRegistration(
                handler,
                definition.IsReplayable,
                [],
                [],
                EventSourceType.Unspecified,
                EventStreamType.All,
                handle);
        });
    }

    /// <inheritdoc/>
    public IReactorDefinition Define(ReactorId id, Action<IReactorBuilder> define)
    {
        ArgumentNullException.ThrowIfNull(define);
        var builder = new ReactorBuilder(_eventTypes);
        define(builder);
        return builder.Build(id);
    }

    /// <inheritdoc/>
    public Task<IReactorHandler> Register(ReactorId id, Action<IReactorBuilder> define) => Register(Define(id, define));

    /// <inheritdoc/>
    public Task<IReactorHandler> Register(IReactorDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return AddRegistration(() => CreateRegistrationFor(definition));
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
        where TReactor : IReactor => GetHandlerFor(typeof(TReactor));

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
        var handler = GetHandlerFor(reactorType);
        return handler.GetFailedPartitions();
    }

    /// <inheritdoc/>
    public async Task<ReactorPartitionRetryOutcome> RetryFailedPartitionFor(Type reactorType, Partition partition)
    {
        var handler = GetHandlerFor(reactorType);
        var response = await _servicesAccessor.Services.Observers.RetryPartition(new RetryPartition
        {
            EventStore = _eventStore.Name,
            Namespace = _eventStore.Namespace,
            ObserverId = handler.Id,
            EventSequenceId = handler.EventSequenceId,
            Partition = partition
        });

        return response.Outcome.ToClient();
    }

    /// <inheritdoc/>
    public async Task<ReactorPartitionQuarantineClearResult> ClearFailedPartitionQuarantineFor(Type reactorType, Partition partition, bool retryImmediately = true)
    {
        var handler = GetHandlerFor(reactorType);
        var response = await _servicesAccessor.Services.Observers.ClearPartitionQuarantine(new ClearPartitionQuarantine
        {
            EventStore = _eventStore.Name,
            Namespace = _eventStore.Namespace,
            ObserverId = handler.Id,
            EventSequenceId = handler.EventSequenceId,
            Partition = partition,
            RetryImmediately = retryImmediately
        });

        return response.ToClient();
    }

    /// <inheritdoc/>
    public Task<ReactorState> GetStateFor<TReactor>()
        where TReactor : IReactor
    {
        var reactorType = typeof(TReactor);
        var handler = GetHandlerFor(reactorType);
        return handler.GetState();
    }

    /// <inheritdoc/>
    public Task<JobId> Replay<TReactor>()
        where TReactor : IReactor
    {
        var reactorType = typeof(TReactor);
        var handler = GetHandlerFor(reactorType);
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

    /// <inheritdoc/>
    public Task<FastForwardResult> FastForward<TReactor>()
        where TReactor : IReactor => FastForward(GetHandlerFor<TReactor>().Id);

    /// <inheritdoc/>
    public Task<FastForwardResult> FastForward(ReactorId reactorId) =>
        ObserverFastForwarder.FastForward(_servicesAccessor.Services.Observers, _eventStore.Name, _eventStore.Namespace, reactorId);

    /// <summary>
    /// Add a fluent reactor definition that is built and registered by the next <see cref="Discover"/>.
    /// </summary>
    /// <param name="id">The stable reactor identifier.</param>
    /// <param name="define">Declares the reactor on an <see cref="IReactorBuilder"/>.</param>
    /// <remarks>
    /// This is how a reactor registered while the client is being configured reaches the event store: at that point
    /// the event types it refers to are not known yet, so defining it is left to discovery, which knows them.
    /// </remarks>
    internal void Add(ReactorId id, Action<IReactorBuilder> define)
    {
        lock (_registerLock)
        {
            _pendingDefinitions.Add((id, define));
        }
    }

    /// <summary>
    /// Registers previously registered reactors after reconnect for stores without automatic registration.
    /// </summary>
    /// <returns>A task representing the registration.</returns>
    /// <exception cref="AggregateException">Multiple reactors could not register again.</exception>
    internal Task RegisterAgainAfterReconnect()
    {
        if (_released || !_eventStore.Connection.Lifecycle.IsConnected)
        {
            return Task.CompletedTask;
        }
        if (!_handlers.Values.Any(_ => _.RegisterOnReconnect))
        {
            return Task.CompletedTask;
        }

        List<Exception> failures = [];
        WithCurrentServices(services =>
        {
            if (_released || !_eventStore.Connection.Lifecycle.IsConnected)
            {
                return;
            }

            foreach (var registration in _handlers.Values.Where(ShouldRegisterAfterReconnect))
            {
                var id = registration.Handler.Id;
                _logger.RegisteringReactorAgainAfterReconnect(id);
                try
                {
                    RegisterReactor(registration, services);
                    registration.IsRegistered = true;
                    registration.RegisterOnReconnect = false;
                }
                catch (Exception ex)
                {
                    _logger.RegisteringReactorAgainAfterReconnectFailed(id, ex);
                    failures.Add(ex);
                }
            }
        });

        ThrowRegistrationFailures(failures);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Releases the reactors of an event store whose creation failed and that no caller holds: stops reacting to the
    /// connection lifecycle and closes the observe streams.
    /// </summary>
    /// <remarks>
    /// After release, parameterless <see cref="Register()"/> and reconnect registration are no-ops. Registration
    /// overloads that return a handler throw <see cref="ObjectDisposedException"/>, including registrations still in flight.
    /// </remarks>
    internal void Release()
    {
        lock (_registerLock)
        {
            _released = true;
            _eventStore.Connection.Lifecycle.OnDisconnected -= ConnectionDisconnected;
            DisconnectHandlers();
        }
    }

    static bool ShouldRegisterAfterReconnect(ReactorRegistration registration) =>
        registration.RegisterOnReconnect && !registration.IsRegistered && !registration.Handler.CancellationToken.IsCancellationRequested;

    static void ThrowRegistrationFailures(List<Exception> failures)
    {
        if (failures.Count == 1)
        {
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        }
        if (failures.Count > 1)
        {
            throw new AggregateException(failures);
        }
    }

    static void ThrowIfUnknownReactorId(IReactorHandler? handler, ReactorId reactorId)
    {
        if (handler is null)
        {
            throw new UnknownReactorId(reactorId);
        }
    }

    ReactorRegistration CreateDiscoveredRegistration(Type reactorType)
    {
        var registration = CreateRegistrationFor(reactorType);
        if (_handlers.TryGetValue(registration.Handler.Id, out var previous))
        {
            registration.RegisterOnReconnect = previous.RegisterOnReconnect;
        }
        return registration;
    }

    ReactorRegistration CreateRegistrationFor(Type reactorType)
    {
        var eventTypes = ReactorInvoker.GetEventTypesFor(_eventStore.EventTypes, reactorType, _sideEffectHandlers);
        ObserverHasOnlyGenerationHandlers.ThrowIfEmpty(reactorType, eventTypes);
#pragma warning disable CA2000 // Ownership of the handler transfers to the registration.
        var handler = CreateHandler(
            ResolveId(reactorType),
            reactorType,
            reactorType.GetEventSequenceId(_eventStore.EventTypes, _eventStore.Name, _sideEffectHandlers, _clientArtifactsProvider),
            eventTypes);
#pragma warning restore CA2000
        var eventSourceFilter = reactorType.GetEventSourceFilter(_eventStore.EventSources);
        return new(
            handler,
            !reactorType.IsDefined(typeof(OnceOnlyAttribute), inherit: false),
            [.. reactorType.GetTags()],
            [.. reactorType.GetFilterTags()],
            eventSourceFilter.EventSourceType,
            eventSourceFilter.EventStreamType,
            null,
            reactorType.IsDefined(typeof(StartAtTailAttribute), inherit: false));
    }

    ReactorRegistration CreateRegistrationFor(IReactorDefinition definition)
    {
#pragma warning disable CA2000 // Ownership of the handler transfers to the registration.
        var handler = CreateHandler(definition.Id, typeof(object), definition.EventSequenceId, [.. definition.EventTypes]);
#pragma warning restore CA2000
        return new(
            handler,
            definition.IsReplayable,
            [],
            [],
            EventSourceType.Unspecified,
            EventStreamType.All,
            CreateDispatcherFor(definition));
    }

    /// <summary>
    /// Create the callback that turns a delivered event into the object a fluent definition's handlers receive.
    /// </summary>
    /// <param name="definition">The <see cref="IReactorDefinition"/> to dispatch to.</param>
    /// <returns>The callback.</returns>
    /// <remarks>
    /// The event is deserialized in the generation the definition subscribed to, as it is for a discovered reactor, so
    /// a handler always receives the CLR type it was declared for even when the event was appended in another
    /// generation.
    /// </remarks>
    Func<ReactorEvent, CancellationToken, Task> CreateDispatcherFor(IReactorDefinition definition)
    {
        var generations = definition.EventTypes
            .DistinctBy(_ => _.Id)
            .ToDictionary(_ => _.Id, _ => _.Generation);

        return async (delivered, _) =>
        {
            var context = delivered.Context;
            var content = delivered.Content;
            var generation = generations.TryGetValue(context.EventType.Id, out var subscribed) ? subscribed : context.EventType.Generation;
            if (generation != context.EventType.Generation &&
                delivered.GenerationalContent.TryGetValue((int)generation.Value, out var generationalContent))
            {
                content = JsonNode.Parse(generationalContent)!.AsObject();
                context = context with { EventType = context.EventType with { Generation = generation } };
            }

            var eventType = _eventTypes.GetClrTypeFor(context.EventType.Id, context.EventType.Generation);
            var @event = await _eventSerializer.Deserialize(eventType, content);
            await definition.Handle(@event, context);
        };
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

    void WithCurrentServices(Action<IServices> register)
    {
        while (true)
        {
            long generation;
            lock (_registerLock)
            {
                if (_released)
                {
                    return;
                }
                generation = _connectionGeneration;
            }

            // Resolving services can connect synchronously, so connection callbacks must be able to take the lock.
            var services = _servicesAccessor.Services;
            lock (_registerLock)
            {
                if (_released)
                {
                    return;
                }
                if (generation != _connectionGeneration)
                {
                    continue;
                }
                register(services);
                return;
            }
        }
    }

    Task<IReactorHandler> AddRegistration(Func<ReactorRegistration> create)
    {
        IReactorHandler? handler = null;
        WithCurrentServices(services =>
        {
            var registration = create();
            if (_handlers.ContainsKey(registration.Handler.Id))
            {
                registration.Handler.Disconnect();
                (registration.Handler as IDisposable)?.Dispose();
                throw new ReactorAlreadyRegistered(registration.Handler.Id);
            }

            try
            {
                RegisterReactor(registration, services);
                registration.IsRegistered = true;
                _handlers = _handlers.Append(new KeyValuePair<ReactorId, ReactorRegistration>(registration.Handler.Id, registration))
                    .ToFrozenDictionary();
                handler = registration.Handler;
            }
            catch
            {
                registration.Handler.Disconnect();
                registration.Handler.Dispose();
                throw;
            }
        });
        if (handler is null)
        {
            throw new ObjectDisposedException(nameof(Reactors), "Cannot register a reactor after its event store has been released.");
        }
        return Task.FromResult(handler);
    }

    Task ConnectionDisconnected()
    {
        lock (_registerLock)
        {
            if (_released)
            {
                return Task.CompletedTask;
            }

            _connectionGeneration++;
            _registered = false;
            RecreateHandlersForReconnect();
        }

        return Task.CompletedTask;
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
        _handlers = registrations.Select(registration =>
        {
            var recreated = RecreateRegistration(registration);
            recreated.RegisterOnReconnect = registration.IsRegistered || registration.RegisterOnReconnect;
            return recreated;
        }).ToFrozenDictionary(_ => _.Handler.Id);
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

    /// <summary>
    /// Refuses a reactor that resolves to HTTP delivery, which is not available yet.
    /// </summary>
    /// <param name="handler">The <see cref="ReactorHandler"/> being registered.</param>
    /// <exception cref="HttpDeliveryNotAvailable">Thrown when the reactor resolves to HTTP delivery.</exception>
    void ThrowIfDeliveredOverHttp(ReactorHandler handler)
    {
        if (Deliveries?.Resolve(handler.Id.Value, handler.ReactorType) == ObserverDelivery.Http)
        {
            throw new HttpDeliveryNotAvailable([handler.Id.Value]);
        }
    }

    bool IsCurrentHandler(ReactorHandler handler) =>
        _handlers.TryGetValue(handler.Id, out var current) && ReferenceEquals(current.Handler, handler);

    void ReconnectIfCurrent(ReactorRegistration registration, CancellationToken cancellationToken)
    {
        var handler = registration.Handler;

        WithCurrentServices(services =>
        {
            if (cancellationToken.IsCancellationRequested || _released || !IsCurrentHandler(handler))
            {
                _logger.SkippingStaleReactorReconnect(handler.Id);
                return;
            }

            _logger.ReconnectingReactor(handler.Id);
            RegisterReactor(registration, services);
        });
    }

    void RegisterReactor(ReactorRegistration registration, IServices services)
    {
        var handler = registration.Handler;
        ThrowIfDeliveredOverHttp(handler);
        ClientObserverHosting.ThrowIfUnavailable(_eventStore.Connection, "Reactor", () => handler.Id.Value);
        if (GenerationDelivery == Observation.EventGenerationDelivery.Pinned)
        {
            GenerationDeliveryValidation.Validate(handler.EventTypes);
        }

        _logger.RegisteringReactor(handler.Id);
        var request = new RegisterReactor
        {
            ConnectionId = _eventStore.Connection.Lifecycle.ConnectionId,
            EventStore = _eventStore.Name,
            Namespace = _eventStore.Namespace,
            PageDelivery = new() { Enabled = true },
            Reactor = new ReactorDefinition
            {
                GenerationDelivery = (Contracts.Observation.EventGenerationDelivery)GenerationDelivery,
                ReactorId = handler.Id,
                EventSequenceId = handler.EventSequenceId,
                EventTypes = handler.EventTypes.Select(et => new EventTypeWithKeyExpression { EventType = et.ToContract(), Key = WellKnownExpressions.EventSourceId }).ToArray(),
                IsReplayable = registration.IsReplayable,
                IsNotReplayable = !registration.IsReplayable,
                StartAtTail = registration.StartAtTail,
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
        var messages = new SerializedMessages<ReactorMessage>(new(new(request)));
#pragma warning restore CA2000 // Dispose objects before losing scope
        var cancellationToken = handler.CancellationToken;
#pragma warning disable CA2000 // Dispose objects before losing scope
        var scheduler = new PartitionedDeliveryScheduler(MaxConcurrentPartitions);
#pragma warning restore CA2000 // Dispose objects before losing scope
#if CHRONICLE_ROLLING_DIAGNOSTICS
        var diagnostic = ReactorsDeliveryDiagnosticsLogging.Open(_logger);
#endif
        var eventsToObserve = services.Reactors.Observe(messages.Messages, cancellationToken);

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

            _ = ScheduleReconnectWork(async () =>
            {
                try
                {
                    await scheduler.DrainAndDispose();
                    await Task.Delay(ReconnectDelay, cancellationToken);
                    ReconnectIfCurrent(registration, cancellationToken);
                }
                catch (OperationCanceledException ex)
                {
                    _logger.RegisteringReactorStreamCancelled(handler.Id, ex);
                }
                catch (Exception ex)
                {
                    _logger.ReactorReconnectFailed(handler.Id, ex);
                }
            });
        }

        // https://github.com/dotnet/reactive/issues/459
        eventsToObserve
            .Select(events =>
            {
#if CHRONICLE_ROLLING_DIAGNOSTICS
                var batch = ReactorsDeliveryDiagnosticsLogging.NextBatch(_logger, diagnostic.Stream, ref diagnostic.Batch, events);
#endif
                async Task Deliver()
                {
#if CHRONICLE_ROLLING_DIAGNOSTICS
                    await ObserverMethod(messages, registration, (events, diagnostic.Stream, batch), cancellationToken);
#else
                    await ObserverMethod(messages, registration, events, cancellationToken);
#endif
                    _logger.EventHandlingCompleted(handler.Id);
                }

                var isBarrier = events.ReplayState is ReplayState.BeginReplay or ReplayState.EndReplay;
                var partition = events.SchedulingPartition();
                return (isBarrier || partition is null ? scheduler.ScheduleBarrier(Deliver) : scheduler.Schedule(partition, Deliver)).ToObservable();
            })
            .Merge()
            .Subscribe(
                _ => { },
                ex =>
                {
                    scheduler.Stop();
                    var errorKind = cancellationToken.IsCancellationRequested ? KernelConnectionErrorKind.Cancelled : ex.ClassifyStreamError();
#if CHRONICLE_ROLLING_DIAGNOSTICS
                    ReactorsDeliveryDiagnosticsLogging.StreamError(_logger, diagnostic.Stream, errorKind == KernelConnectionErrorKind.Cancelled);
#endif
                    if (errorKind == KernelConnectionErrorKind.Cancelled)
                    {
                        _logger.RegisteringReactorStreamCancelled(handler.Id, ex);
                        messages.Dispose();
                        _ = scheduler.DrainAndDispose();
                        return;
                    }

                    // The kernel is stopping or unreachable. This stays a Warning however long it lasts: a kernel that
                    // stays away is reported at Error by the connection watchdog on every failed reconnect attempt.
                    if (errorKind == KernelConnectionErrorKind.ConnectionLost)
                    {
                        _logger.ReactorStreamLostConnection(handler.Id, ex);
                        messages.Dispose();
                        ScheduleReconnect();
                        return;
                    }

                    // The kernel lost this client's registration. Retrying the same stream with the same connection id
                    // can never work, so reconnect the whole client, which re-registers every reactor.
                    if (errorKind == KernelConnectionErrorKind.SessionLost)
                    {
                        _logger.ReactorStreamLostConnection(handler.Id, ex);
                        messages.Dispose();
                        _ = scheduler.DrainAndDispose();
                        _eventStore.Connection.RequestReconnect();
                        return;
                    }

                    var streamFailed = new ReactorObservationStreamFailed(handler.Id, ex);
                    _logger.RegisteringReactorFailed(handler.Id, streamFailed);
                    messages.Dispose();
                    ScheduleReconnect();
                },
                () =>
                {
                    scheduler.Stop();
#if CHRONICLE_ROLLING_DIAGNOSTICS
                    ReactorsDeliveryDiagnosticsLogging.Record(_logger, 2, diagnostic.Stream);
#endif
                    _logger.RegisteringReactorStreamCompleted(handler.Id);
                    messages.Dispose();
                    ScheduleReconnect();
                });
    }

#if CHRONICLE_ROLLING_DIAGNOSTICS
    Task ObserverMethod(SerializedMessages<ReactorMessage> messages, ReactorRegistration registration, (EventsToObserve Events, Guid Stream, long Batch) delivery, CancellationToken cancellationToken) =>
        _dispatcher.Dispatch(registration.Handler, registration.Handle, delivery.Events, _eventStore.Namespace, result => messages.Publish(new(new(result))), cancellationToken, delivery.Stream, delivery.Batch);
#else
    Task ObserverMethod(SerializedMessages<ReactorMessage> messages, ReactorRegistration registration, EventsToObserve events, CancellationToken cancellationToken) =>
        _dispatcher.Dispatch(registration.Handler, registration.Handle, events, _eventStore.Namespace, result => messages.Publish(new(new(result))), cancellationToken);
#endif

    sealed record ReactorRegistration(
        ReactorHandler Handler,
        bool IsReplayable,
        string[] Tags,
        string[] FilterTags,
        EventSourceType EventSourceType,
        EventStreamType EventStreamType,
        Func<ReactorEvent, CancellationToken, Task>? Handle,
        bool StartAtTail = false)
    {
        /// <summary>
        /// Gets or sets whether the subscription has been sent on the current connection.
        /// </summary>
        public bool IsRegistered { get; set; }

        /// <summary>
        /// Gets or sets whether the reactor was registered on a dropped connection and not yet on the current one.
        /// </summary>
        public bool RegisterOnReconnect { get; set; }
    }
}
