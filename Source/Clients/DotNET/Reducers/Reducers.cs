// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Receivers;
using Cratis.Chronicle.Serverless;
using Cratis.Chronicle.Transactions;
using Cratis.Serialization;
using Cratis.Traces;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducers"/>.
/// </summary>
public partial class Reducers : IReducers, IReceiveReducerDeliveries
{
    static readonly ConcurrentDictionary<Type, Lazy<string>> _fingerprints = new();
    readonly IChronicleServicesAccessor _servicesAccessor;
    readonly IEventStore _eventStore;
    readonly IClientArtifactsProvider _clientArtifacts;
    readonly IServiceProvider _serviceProvider;
    readonly IClientArtifactsActivator _artifactActivator;
    readonly IReducerValidator _reducerValidator;
    readonly IEventTypes _eventTypes;
    readonly INamingPolicy _namingPolicy;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly ReducerDispatcher _dispatcher;
    readonly ILogger<Reducers> _logger;
    readonly IReducerObservers _reducerObservers;
    IReadOnlyDictionary<Type, IReducerHandler> _handlersByType = FrozenDictionary<Type, IReducerHandler>.Empty;
    IReadOnlyDictionary<Type, IReducerHandler> _handlersByModelType = FrozenDictionary<Type, IReducerHandler>.Empty;

    bool _registered;

    /// <summary>
    /// Initializes a new instance of the <see cref="Reducers"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="IEventStore"/> the reducers belong to.</param>
    /// <param name="clientArtifacts"><see cref="IClientArtifactsProvider"/> for discovery.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> to get instances of types.</param>
    /// <param name="artifactActivator"><see cref="IClientArtifactsActivator"/> for creating artifact instances.</param>
    /// <param name="reducerValidator"><see cref="IReducerValidator"/> for validating reducer types.</param>
    /// <param name="eventTypes">Registered <see cref="IEventTypes"/>.</param>
    /// <param name="namingPolicy"><see cref="INamingPolicy"/> for converting names during serialization.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for JSON serialization.</param>
    /// <param name="identityProvider"><see cref="IIdentityProvider"/> for managing identity context.</param>
    /// <param name="reducerObservers"><see cref="IReducerObservers"/> for managing reducer observers.</param>
    /// <param name="activitySource"><see cref="IActivitySource{T}"/> for tracing reducer event handling.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public Reducers(
        IEventStore eventStore,
        IClientArtifactsProvider clientArtifacts,
        IServiceProvider serviceProvider,
        IClientArtifactsActivator artifactActivator,
        IReducerValidator reducerValidator,
        IEventTypes eventTypes,
        INamingPolicy namingPolicy,
        JsonSerializerOptions jsonSerializerOptions,
        IIdentityProvider identityProvider,
        IReducerObservers reducerObservers,
        IActivitySource<Reducers> activitySource,
        ILogger<Reducers> logger)
    {
        eventStore.Connection.Lifecycle.OnDisconnected += () =>
        {
            lock (ReadModelBuilderOwnership.SyncFor(eventStore, ReadModelOwnershipSync))
            {
                _registered = false;
                RecreateHandlersForReconnect();
            }

            return Task.CompletedTask;
        };
        _eventStore = eventStore;
        _servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;
        _clientArtifacts = clientArtifacts;
        _serviceProvider = serviceProvider;
        _artifactActivator = artifactActivator;
        _reducerValidator = reducerValidator;
        _eventTypes = eventTypes;
        _namingPolicy = namingPolicy;
        _jsonSerializerOptions = jsonSerializerOptions;
        _reducerObservers = reducerObservers;
        _logger = logger;
        _dispatcher = new ReducerDispatcher(
            eventStore,
            serviceProvider,
            artifactActivator,
            eventTypes,
            jsonSerializerOptions,
            identityProvider,
            activitySource,
            logger);
    }

    /// <summary>
    /// Gets the fallback monitor used by native registries on custom stores.
    /// The shared ownership monitor also serializes registration, reconnect and cancellation writes; no second lock is acquired.
    /// </summary>
    internal object ReadModelOwnershipSync { get; } = new();

    /// <summary>
    /// Gets the generation delivery policy supplied by the event store.
    /// </summary>
    internal Observation.EventGenerationDelivery GenerationDelivery { get; init; }

    /// <summary>
    /// Gets how the events of reducers are delivered, when supplied by the event store.
    /// </summary>
    internal ObserverDeliveries? Deliveries { get; init; }

    /// <summary>
    /// Gets how many partitions of one reducer are handled at the same time.
    /// </summary>
    internal int MaxConcurrentPartitions { get; init; } = PartitionedDeliveryScheduler.DefaultMaxConcurrentPartitions;

    /// <inheritdoc/>
    public Task Discover()
    {
        lock (ReadModelBuilderOwnership.SyncFor(_eventStore, ReadModelOwnershipSync))
        {
            return DiscoverCore();
        }
    }

    /// <inheritdoc/>
    public async Task Register()
    {
        lock (ReadModelBuilderOwnership.SyncFor(_eventStore, ReadModelOwnershipSync))
        {
            if (_registered)
            {
                return;
            }

            _logger.RegisterReducers();

            foreach (var handler in _handlersByModelType.Values.Where(_ => _.IsActive))
            {
                RegisterWhenDispatchable(handler);
            }
            _registered = true;
        }
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReducerHandler> Register<TReducer, TReadModel>()
        where TReducer : IReducerFor<TReadModel>
        where TReadModel : class
    {
        var reducerType = typeof(TReducer);
        var modelType = typeof(TReadModel);
        lock (ReadModelBuilderOwnership.SyncFor(_eventStore, ReadModelOwnershipSync))
        {
            ReadModelBuilderOwnership.Validate(_eventStore, _clientArtifacts, Catalog, modelType, reducerType, false);
            var handler = CreateHandlerFor(reducerType, modelType);
            RegisterWhenDispatchable(handler);
            var handlersByType = new Dictionary<Type, IReducerHandler>(_handlersByType) { { reducerType, handler } };
            var handlersByModelType = new Dictionary<Type, IReducerHandler>(_handlersByModelType) { { modelType, handler } };
            _handlersByType = handlersByType.ToFrozenDictionary();
            _handlersByModelType = handlersByModelType.ToFrozenDictionary();
            return Task.FromResult<IReducerHandler>(handler);
        }
    }

    /// <inheritdoc/>
    public IEnumerable<IReducerHandler> GetAllHandlers() => _handlersByModelType.Values;

    /// <inheritdoc/>
    public IReducerHandler GetHandlerById(ReducerId reducerId)
    {
        var reducer = _handlersByModelType.Values.SingleOrDefault(_ => _.Id == reducerId);
        ReducerDoesNotExist.ThrowIfDoesNotExist(reducerId, reducer);
        return reducer!;
    }

    /// <inheritdoc/>
    public IReducerHandler GetHandlerFor(Type reducerType)
    {
        ThrowIfTypeIsNotAReducer(reducerType);
        return _handlersByType[reducerType];
    }

    /// <inheritdoc/>
    public IReducerHandler GetHandlerFor<TReducer>()
        where TReducer : IReducer => GetHandlerFor(typeof(TReducer));

    /// <inheritdoc/>
    public Type GetClrType(ReducerId reducerId)
    {
        var reducer = _handlersByModelType.SingleOrDefault(_ => _.Value.Id == reducerId);
        ReducerDoesNotExist.ThrowIfDoesNotExist(reducerId, reducer.Value);
        return reducer.Key;
    }

    /// <inheritdoc/>
    public IReducerHandler GetHandlerForReadModelType(Type readModelType) => _handlersByModelType[readModelType];

    /// <inheritdoc/>
    public bool HasReducerFor(Type readModelType) => _handlersByModelType.ContainsKey(readModelType);

    /// <inheritdoc/>
    public bool HasFor<TReadModel>() => HasFor(typeof(TReadModel));

    /// <inheritdoc/>
    public bool HasFor(Type readModelType) => _handlersByModelType.ContainsKey(readModelType);

    /// <inheritdoc/>
    public Task<IEnumerable<Observation.FailedPartition>> GetFailedPartitionsFor<TReducer>()
        where TReducer : IReducer =>
            GetFailedPartitionsFor(typeof(TReducer));

    /// <inheritdoc/>
    public Task<IEnumerable<Observation.FailedPartition>> GetFailedPartitionsFor(Type reducerType)
    {
        var handler = GetHandlerFor(reducerType);
        return handler.GetFailedPartitions();
    }

    /// <inheritdoc/>
    public Task<ReducerState> GetStateFor<TReducer>()
        where TReducer : IReducer
    {
        var reducerType = typeof(TReducer);
        var handler = _handlersByType[reducerType];
        return handler.GetState();
    }

    /// <inheritdoc/>
    public Task<JobId> Replay<TReducer>()
        where TReducer : IReducer
    {
        var reducerType = typeof(TReducer);
        var handler = _handlersByType[reducerType];
        return Replay(handler.Id);
    }

    /// <inheritdoc/>
    public async Task<JobId> Replay(ReducerId reducerId)
    {
        var response = await _servicesAccessor.Services.Observers.Replay(new()
        {
            EventStore = _eventStore.Name,
            Namespace = _eventStore.Namespace,
            ObserverId = reducerId,
            EventSequenceId = string.Empty
        });
        return Guid.TryParse(response.JobId, out var value) ? new JobId(value) : JobId.NotSet;
    }

    /// <inheritdoc/>
    public Task<FastForwardResult> FastForward<TReducer>()
        where TReducer : IReducer => FastForward(_handlersByType[typeof(TReducer)].Id);

    /// <inheritdoc/>
    public Task<FastForwardResult> FastForward(ReducerId reducerId) =>
        ObserverFastForwarder.FastForward(_servicesAccessor.Services.Observers, _eventStore.Name, _eventStore.Namespace, reducerId);

    /// <inheritdoc/>
    public async Task<object?> GetInstanceById(Type readModelType, ReadModelKey readModelKey)
    {
        ReadProtectionGuard.Read(nameof(GetInstanceById));
        var handler = GetHandlerForReadModelType(readModelType);
        var filter = ObserverEventFilter.For(handler.ReducerType, _eventStore.EventSources);
        var events = (await _eventStore
            .GetEventSequence(handler.EventSequenceId)
            .GetForEventSourceIdAndEventTypes((EventSourceId)readModelKey.Value, handler.EventTypes))
            .Where(@event => filter.Matches(@event.Context)).ToList();

        if (events.Count == 0)
        {
            return null;
        }

        var reduceResult = await handler.Invoker.Invoke(
            _serviceProvider,
            events
                .OrderBy(@event => @event.Context.SequenceNumber)
                .Select(@event => new EventAndContext(@event.Content, @event.Context)),
            null);

        if (!reduceResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to reduce read model '{readModelType.Name}'. {string.Join(Environment.NewLine, reduceResult.ErrorMessages)}");
        }

        return reduceResult.ReadModelState;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<object>> GetInstances(Type readModelType, EventCount? eventCount = null)
    {
        ReadProtectionGuard.Read(nameof(GetInstances));
        var handler = GetHandlerForReadModelType(readModelType);
        var events = await _eventStore
            .GetEventSequence(handler.EventSequenceId)
            .GetFromSequenceNumber(EventSequenceNumber.First, filterEventTypes: handler.EventTypes);

        var orderedEvents = events.OrderBy(_ => _.Context.SequenceNumber);
        IEnumerable<AppendedEvent> eventsToReduce = eventCount is { Value: not ulong.MaxValue }
            ? orderedEvents.Take(eventCount.Value > int.MaxValue ? int.MaxValue : (int)eventCount.Value).ToList()
            : orderedEvents;

        var filter = ObserverEventFilter.For(handler.ReducerType, _eventStore.EventSources);
        var result = new List<object>();
        foreach (var eventsForEventSource in eventsToReduce
                     .Where(@event => filter.Matches(@event.Context))
                     .GroupBy(_ => _.Context.EventSourceId)
                     .Select(group => group.ToList()))
        {
            var reduceResult = await handler.Invoker.Invoke(
                _serviceProvider,
                eventsForEventSource.Select(@event => new EventAndContext(@event.Content, @event.Context)),
                null);

            if (!reduceResult.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to reduce read model '{readModelType.Name}'. {string.Join(Environment.NewLine, reduceResult.ErrorMessages)}");
            }

            if (reduceResult.ReadModelState is not null)
            {
                result.Add(reduceResult.ReadModelState);
            }
        }

        return result;
    }

    /// <inheritdoc/>
    async Task<ReducerResult?> IReceiveReducerDeliveries.Deliver(ReducerId id, ReduceOperationMessage operation)
    {
        var handler = _handlersByModelType.Values.SingleOrDefault(_ => _.Id == id && _.IsActive);
        ReducerDoesNotExist.ThrowIfDoesNotExist(id, handler);

        ReducerResult? result = null;
        await _dispatcher.Dispatch(handler!, operation, _eventStore.Namespace, published => result = published);
        return result;
    }

    /// <summary>
    /// Get snapshots for a reduced read model by its key.
    /// </summary>
    /// <typeparam name="TReadModel">Type of the read model.</typeparam>
    /// <param name="readModelKey">The <see cref="ReadModelKey"/> to get snapshots for.</param>
    /// <returns>Collection of <see cref="ReadModelSnapshot{TReadModel}"/>.</returns>
    internal async Task<IEnumerable<ReadModelSnapshot<TReadModel>>> GetSnapshotsById<TReadModel>(ReadModelKey readModelKey)
    {
        var readModelType = typeof(TReadModel);
        var handler = GetHandlerForReadModelType(readModelType);

        var response = await _servicesAccessor.Services.ReadModelExplorer.AllSnapshotsForReadModel(
            new Contracts.ReadModelExplorer.AllSnapshotsForReadModelRequest
            {
                EventStore = _eventStore.Name,
                Namespace = _eventStore.Namespace,
                ReadModel = readModelType.GetReadModelIdentifier(),
                ReadModelKey = readModelKey,
                EventSequenceId = handler.EventSequenceId
            });

        return response.Data.Select(snapshot => new ReadModelSnapshot<TReadModel>(
            JsonSerializer.Deserialize<TReadModel>(snapshot.Instance, _jsonSerializerOptions)!,
            snapshot.Events.ToClient(_eventStore.Name, _eventStore.Namespace, _eventTypes, _jsonSerializerOptions),
            snapshot.Occurred,
            snapshot.CorrelationId)).ToList();
    }

    Task DiscoverCore()
    {
        DisconnectHandlers();
        _handlersByType = _clientArtifacts.Reducers
                            .Where(IsDiscoverable)
                            .ToFrozenDictionary(
                                _ => _,
                                reducerType =>
                                {
                                    var readModelType = ResolveReadModel(reducerType);
                                    return CreateHandlerFor(reducerType, readModelType) as IReducerHandler;
                                });

        _handlersByModelType = _handlersByType.ToFrozenDictionary(
            _ => _.Value.ReadModelType,
            _ => _.Value);
        return Task.CompletedTask;
    }

    ReducerHandler CreateHandlerFor(Type reducerType, Type readModelType)
    {
        EventStreamReadModel.RefuseReducer(readModelType);
        EventStreamReadModel.RefuseReducer(reducerType);
        var descriptor = new ReducerDescriptor(reducerType, readModelType, ReducerCatalog.IsSelfReducer(reducerType));
        ReadModelBuilderOwnership.Validate(_eventStore, _clientArtifacts, Catalog, readModelType, reducerType, descriptor.IsSelfReducing);

        // Preserve classic metadata validation order; self inference instead needs its selected static event set.
        var sequence = descriptor.IsSelfReducing ? null : reducerType.GetEventSequenceId(_eventTypes, _eventStore.Name, readModelType);
        var invoker = ReducerInvokerSelector.Create(descriptor, _eventTypes, _artifactActivator, _namingPolicy.GetReadModelName(readModelType));
        ObserverHasOnlyGenerationHandlers.ThrowIfEmpty(reducerType, invoker.EventTypes);
        sequence ??= Registrations.RegisteredObserverMetadata.SequenceFor(reducerType, reducerType.GetCustomAttribute<ReducerAttribute>()?.EventSequenceId, _eventTypes, _eventStore.Name, () => invoker.EventTypes);
        var handler = new ReducerHandler(
            _eventStore,
            ResolveId(reducerType),
            reducerType,
            sequence,
            invoker,
            ShouldReducerBeActive(reducerType, readModelType),
            _reducerObservers);

        CancellationTokenRegistration? register = null;
        register = handler.CancellationToken.Register(() =>
        {
            lock (ReadModelBuilderOwnership.SyncFor(_eventStore, ReadModelOwnershipSync))
            {
                _handlersByType = _handlersByType.Where(_ => !ReferenceEquals(_.Value, handler)).ToFrozenDictionary();
                _handlersByModelType = _handlersByModelType.Where(_ => !ReferenceEquals(_.Value, handler)).ToFrozenDictionary();
                register?.Dispose();
            }
        });

        return handler;
    }

    void DisconnectHandlers()
    {
        foreach (var handler in _handlersByType.Values.ToList())
        {
            handler.Disconnect();
            (handler as IDisposable)?.Dispose();
        }
    }

    void RecreateHandlersForReconnect()
    {
        var reducerTypes = _handlersByType.Values
            .Select(_ => (_.ReducerType, _.ReadModelType))
            .ToArray();

        DisconnectHandlers();

        _handlersByType = reducerTypes.ToFrozenDictionary(
            _ => _.ReducerType,
            _ => CreateHandlerFor(_.ReducerType, _.ReadModelType) as IReducerHandler);
        _handlersByModelType = _handlersByType.ToFrozenDictionary(
            _ => _.Value.ReadModelType,
            _ => _.Value);
    }

    bool IsDiscoverable(Type type)
    {
        if (!Catalog.IsDiscoverable(type))
        {
            _logger.SelfReducerNearMiss(type);
            return false;
        }

        return true;
    }

    void RegisterWhenDispatchable(IReducerHandler handler)
    {
        if (handler.EventTypes.Any())
        {
            RegisterReducerWhenHostable(handler);
            return;
        }

        _logger.ReducerHasNoDispatchableMethods(
            handler.ReducerType,
            SkippedReducerMethods.Describe(handler.ReducerType, handler.ReadModelType, _eventTypes.AllClrTypes));
    }

    /// <summary>
    /// Refuses a reducer that resolves to HTTP delivery, which is not available yet.
    /// </summary>
    /// <param name="handler">The <see cref="IReducerHandler"/> being registered.</param>
    /// <exception cref="HttpDeliveryNotAvailable">Thrown when the reducer resolves to HTTP delivery.</exception>
    void ThrowIfDeliveredOverHttp(IReducerHandler handler)
    {
        if (Deliveries?.Resolve(handler.Id.Value, handler.ReducerType) == ObserverDelivery.Http)
        {
            throw new HttpDeliveryNotAvailable([handler.Id.Value]);
        }
    }

    void RegisterReducerWhenHostable(IReducerHandler handler)
    {
        ThrowIfDeliveredOverHttp(handler);
        ClientObserverHosting.ThrowIfUnavailable(_eventStore.Connection, "Reducer", () => handler.Id.Value);
        RegisterReducer(handler);
    }

    void RegisterReducer(IReducerHandler handler)
    {
        if (GenerationDelivery == Observation.EventGenerationDelivery.Pinned)
        {
            GenerationDeliveryValidation.Validate(handler.EventTypes);
        }

        _logger.RegisterReducer(
            handler.Id,
            handler.EventSequenceId);

        var filter = ObserverEventFilter.For(handler.ReducerType, _eventStore.EventSources);
        var registration = new RegisterReducer
        {
            ConnectionId = _eventStore.Connection.Lifecycle.ConnectionId,
            EventStore = _eventStore.Name,
            Namespace = _eventStore.Namespace,
            Reducer = new ReducerDefinition
            {
                GenerationDelivery = (Contracts.Observation.EventGenerationDelivery)GenerationDelivery,
                ReducerId = handler.Id,
                EventSequenceId = handler.EventSequenceId,
                EventTypes = handler.EventTypes.Select(et => new EventTypeWithKeyExpression { EventType = et.ToContract(), Key = WellKnownExpressions.EventSourceId }).ToArray(),
                ReadModel = handler.ReadModelType.GetReadModelIdentifier(),
                IsActive = handler.IsActive,
                Tags = handler.ReducerType.GetTags().ToArray(),
                Filters = new()
                {
                    FilterTags = filter.FilterTags.ToArray(),
                    EventSourceType = filter.EventSourceType.Value,
                    EventStreamType = filter.EventStreamType.Value
                },
                Hash = GetFingerprint(handler.ReducerType)
            }
        };

#pragma warning disable CA2000 // Dispose objects before losing scope
        var messages = new SerializedMessages<ReducerMessage>(new(new(registration)));
#pragma warning restore CA2000 // Dispose objects before losing scope

        #pragma warning disable CA2000 // Dispose objects before losing scope
        var scheduler = new PartitionedDeliveryScheduler(MaxConcurrentPartitions);
#pragma warning restore CA2000 // Dispose objects before losing scope
        var operationsToObserve = _servicesAccessor.Services.Reducers.Observe(messages.Messages, handler.CancellationToken);

        // https://github.com/dotnet/reactive/issues/459
        operationsToObserve
            .Select(events =>
            {
                async Task Deliver()
                {
                    await ObserverMethod(messages, handler, events);
                    _logger.EventHandlingCompleted(handler.Id);
                }

                var isBarrier = events.ReplayState is ReplayState.BeginReplay or ReplayState.EndReplay;
                return (isBarrier ? scheduler.ScheduleBarrier(Deliver) : scheduler.Schedule(events.Partition ?? string.Empty, Deliver)).ToObservable();
            })
            .Merge()
            .Subscribe(
                _ => { },
                ex =>
                {
                    scheduler.Stop();
                    messages.Dispose();

                    if (handler.CancellationToken.IsCancellationRequested)
                    {
                        _logger.ReducerStreamCancelled(handler.Id, ex);
                        _ = scheduler.DrainAndDispose();
                        return;
                    }

                    // Any other error reconnects, whatever its kind: only the reducer's own cancellation stops it.
                    // A lost kernel connection stays a Warning however long it lasts: a kernel that stays away is
                    // reported at Error by the connection watchdog on every failed reconnect attempt.
                    switch (ex.ClassifyStreamError())
                    {
                        case KernelConnectionErrorKind.Cancelled:
                            _logger.ReducerStreamCancelled(handler.Id, ex);
                            break;
                        case KernelConnectionErrorKind.ConnectionLost:
                            _logger.ReducerStreamLostConnection(handler.Id, ex);
                            break;
                        case KernelConnectionErrorKind.SessionLost:
                            // The kernel lost this client's registration: reconnect the whole client, which
                            // re-registers every reducer with a fresh connection id.
                            _logger.ReducerStreamLostConnection(handler.Id, ex);
                            _ = scheduler.DrainAndDispose();
                            _eventStore.Connection.RequestReconnect();
                            return;
                        default:
                            _logger.ReducerStreamFailed(handler.Id, ex);
                            break;
                    }

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await scheduler.DrainAndDispose();
                            await Task.Delay(TimeSpan.FromSeconds(2), handler.CancellationToken);
                            _logger.ReconnectingReducer(handler.Id);
                            RegisterReducerWhenHostable(handler);
                        }
                        catch (OperationCanceledException) { }
                    });
                });
    }

    string GetFingerprint(Type reducerType) => ReducerCatalog.IsSelfReducer(reducerType)
        ? CreateSelfFingerprint(reducerType)
        : _fingerprints.GetOrAdd(
            reducerType,
            static (type, reducers) => new Lazy<string>(() => reducers.CreateFingerprint(type)),
            this).Value;

    string CreateSelfFingerprint(Type model)
    {
        try
        {
            return SelfReducerFingerprint.Create(_eventTypes, model);
        }
        catch (Exception error)
        {
            _logger.ReducerFingerprintFailed(model, error);
            return SelfReducerFingerprint.CreateFallback(_eventTypes, model);
        }
    }

    string CreateFingerprint(Type reducerType)
    {
        try
        {
            return ReducerFingerprint.Create(reducerType);
        }
        catch (Exception exception)
        {
            _logger.ReducerFingerprintFailed(reducerType, exception);
            return ReducerFingerprint.CreateFallback(reducerType);
        }
    }

    Task ObserverMethod(SerializedMessages<ReducerMessage> messages, IReducerHandler handler, ReduceOperationMessage operation) =>
        _dispatcher.Dispatch(handler, operation, _eventStore.Namespace, result => messages.Publish(new(new(result))));

    bool ShouldReducerBeActive(Type reducerType, Type readModelType)
    {
        var active = reducerType.GetCustomAttribute<ReducerAttribute>()?.IsActive ?? true;
        if (!active)
        {
            return false;
        }

        if (readModelType.IsPassive())
        {
            return false;
        }

        return active;
    }

    void ThrowIfTypeIsNotAReducer(Type reducerType)
    {
        if (!_handlersByType.ContainsKey(reducerType))
        {
            throw new UnknownReducerType(reducerType);
        }
    }
}
