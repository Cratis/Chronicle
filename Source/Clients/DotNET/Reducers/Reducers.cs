// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Contracts.Sinks;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Sinks;
using Cratis.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducers"/>.
/// </summary>
public class Reducers : IReducers
{
#if NET8_0
    static readonly object _registerLock = new();
#else
    static readonly Lock _registerLock = new();
#endif
    readonly IChronicleServicesAccessor _servicesAccessor;
    readonly IEventStore _eventStore;
    readonly IClientArtifactsProvider _clientArtifacts;
    readonly IServiceProvider _serviceProvider;
    readonly IReducerValidator _reducerValidator;
    readonly IEventTypes _eventTypes;
    readonly INamingPolicy _namingPolicy;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IIdentityProvider _identityProvider;
    readonly ILogger<Reducers> _logger;
    readonly IReducerObservers _reducerObservers;
    Dictionary<Type, IReducerHandler> _handlersByType = new();
    Dictionary<Type, IReducerHandler> _handlersByModelType = new();

    bool _registered;

    /// <summary>
    /// Initializes a new instance of the <see cref="Reducers"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="IEventStore"/> the reducers belong to.</param>
    /// <param name="clientArtifacts"><see cref="IClientArtifactsProvider"/> for discovery.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> to get instances of types.</param>
    /// <param name="reducerValidator"><see cref="IReducerValidator"/> for validating reducer types.</param>
    /// <param name="eventTypes">Registered <see cref="IEventTypes"/>.</param>
    /// <param name="namingPolicy"><see cref="INamingPolicy"/> for converting names during serialization.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for JSON serialization.</param>
    /// <param name="identityProvider"><see cref="IIdentityProvider"/> for managing identity context.</param>
    /// <param name="reducerObservers"><see cref="IReducerObservers"/> for managing reducer observers.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public Reducers(
        IEventStore eventStore,
        IClientArtifactsProvider clientArtifacts,
        IServiceProvider serviceProvider,
        IReducerValidator reducerValidator,
        IEventTypes eventTypes,
        INamingPolicy namingPolicy,
        JsonSerializerOptions jsonSerializerOptions,
        IIdentityProvider identityProvider,
        IReducerObservers reducerObservers,
        ILogger<Reducers> logger)
    {
        eventStore.Connection.Lifecycle.OnDisconnected += () =>
        {
            _registered = false;
            return Task.CompletedTask;
        };
        _eventStore = eventStore;
        _servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;
        _clientArtifacts = clientArtifacts;
        _serviceProvider = serviceProvider;
        _reducerValidator = reducerValidator;
        _eventTypes = eventTypes;
        _namingPolicy = namingPolicy;
        _jsonSerializerOptions = jsonSerializerOptions;
        _identityProvider = identityProvider;
        _reducerObservers = reducerObservers;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task Discover()
    {
        _handlersByType = _clientArtifacts.Reducers
                            .ToDictionary(
                                _ => _,
                                reducerType =>
                                {
                                    var readModelType = reducerType.GetReadModelType();
                                    _reducerValidator.Validate(reducerType);
                                    var eventSequenceId = reducerType.GetEventSequenceId();
                                    return CreateHandlerFor(reducerType, readModelType) as IReducerHandler;
                                });

        _handlersByModelType = _handlersByType.ToDictionary(
            _ => _.Value.ReadModelType,
            _ => _.Value);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Register()
    {
        if (_registered)
        {
            return;
        }

        lock (_registerLock)
        {
            if (_registered)
            {
                return;
            }

            _logger.RegisterReducers();

            foreach (var handler in _handlersByModelType.Values.Where(_ => _.IsActive))
            {
                RegisterReducer(handler);
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
        var handler = CreateHandlerFor(reducerType, modelType);
        RegisterReducer(handler);
        _handlersByType.Add(reducerType, handler);
        _handlersByModelType.Add(modelType, handler);
        return Task.FromResult<IReducerHandler>(handler);
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
    public Task Replay<TReducer>()
        where TReducer : IReducer
    {
        var reducerType = typeof(TReducer);
        var handler = _handlersByType[reducerType];
        return Replay(handler.Id);
    }

    /// <inheritdoc/>
    public Task Replay(ReducerId reducerId)
    {
        return _servicesAccessor.Services.Observers.Replay(new()
        {
            EventStore = _eventStore.Name,
            Namespace = _eventStore.Namespace,
            ObserverId = reducerId,
            EventSequenceId = string.Empty
        });
    }

    ReducerHandler CreateHandlerFor(Type reducerType, Type readModelType)
    {
        var handler = new ReducerHandler(
            _eventStore,
            reducerType.GetReducerId(),
            reducerType,
            reducerType.GetEventSequenceId(),
            new ReducerInvoker(
                _eventTypes,
                reducerType,
                readModelType,
                _namingPolicy.GetReadModelName(readModelType)),
            ShouldReducerBeActive(reducerType),
            _reducerObservers);

        CancellationTokenRegistration? register = null;
        register = handler.CancellationToken.Register(() =>
        {
            _handlersByType.Remove(reducerType);
            _handlersByModelType.Remove(readModelType);
            register?.Dispose();
        });

        return handler;
    }

    void RegisterReducer(IReducerHandler handler)
    {
        _logger.RegisterReducer(
            handler.Id,
            handler.EventSequenceId);

        var registration = new RegisterReducer
        {
            ConnectionId = _eventStore.Connection.Lifecycle.ConnectionId,
            EventStore = _eventStore.Name,
            Namespace = _eventStore.Namespace,
            Reducer = new ReducerDefinition
            {
                ReducerId = handler.Id,
                EventSequenceId = handler.EventSequenceId,
                EventTypes = handler.EventTypes.Select(et => new EventTypeWithKeyExpression { EventType = et.ToContract(), Key = WellKnownExpressions.EventSourceId }).ToArray(),
                ReadModel = handler.ReadModelType.GetReadModelIdentifier(),
                IsActive = handler.IsActive,
                Sink = new SinkDefinition
                {
                    TypeId = WellKnownSinkTypes.MongoDB
                },
                Tags = handler.ReducerType.GetTags().ToArray()
            }
        };

#pragma warning disable CA2000 // Dispose objects before losing scope
        var messages = new BehaviorSubject<ReducerMessage>(new(new(registration)));
#pragma warning restore CA2000 // Dispose objects before losing scope

        var operationsToObserve = _servicesAccessor.Services.Reducers.Observe(messages);

        // https://github.com/dotnet/reactive/issues/459
        operationsToObserve
            .Select(events => Observable.FromAsync(async () =>
            {
                await ObserverMethod(messages, handler, events);
                _logger.EventHandlingCompleted(handler.Id);
            }))
            .Concat()
            .Subscribe(_ => { }, messages.Dispose);
    }

    async Task ObserverMethod(BehaviorSubject<ReducerMessage> messages, IReducerHandler handler, ReduceOperationMessage operation)
    {
        var lastSuccessfullyObservedEvent = EventSequenceNumber.Unavailable;
        var exceptionMessages = Enumerable.Empty<string>();
        var exceptionStackTrace = string.Empty;
        var state = ObservationState.Success;
        string? modelState = null;

        var appendedEvents = operation.Events.Select(@event =>
        {
            var context = @event.Context.ToClient();
            var eventType = _eventTypes.GetClrTypeFor(context.EventType.Id);
            var content = JsonSerializer.Deserialize(@event.Content, eventType, _jsonSerializerOptions)!;
            return new AppendedEvent(
                context,
                content);
        }).ToList();

        try
        {
            await using var serviceProviderScope = _serviceProvider.CreateAsyncScope();
            _identityProvider.SetCurrentIdentity(Identity.System);
            var initialState = operation.InitialState is null ? null : JsonSerializer.Deserialize(operation.InitialState, handler.ReadModelType, _jsonSerializerOptions);
            var reduceResult = await handler.OnNext(appendedEvents, initialState, serviceProviderScope.ServiceProvider);

            lastSuccessfullyObservedEvent = reduceResult.LastSuccessfullyObservedEvent;
            if (reduceResult.IsSuccess)
            {
                modelState = reduceResult.ReadModelState is null ?
                    null :
                    JsonSerializer.Serialize(reduceResult.ReadModelState, _jsonSerializerOptions);
            }
            else
            {
                exceptionMessages = reduceResult.ErrorMessages;
                exceptionStackTrace = reduceResult.StackTrace;
                state = ObservationState.Failed;
            }
        }
        catch (Exception ex)
        {
            _logger.ErrorWhileHandlingEvents(ex, appendedEvents[0].Context.SequenceNumber, appendedEvents[^1].Context.SequenceNumber, handler.Id);
            exceptionMessages = ex.GetAllMessages();
            exceptionStackTrace = ex.StackTrace ?? string.Empty;
            state = ObservationState.Failed;
        }

        var result = new ReducerResult
        {
            Partition = operation.Partition,
            ReadModelState = modelState,
            State = state,
            LastSuccessfulObservation = lastSuccessfullyObservedEvent,
            ExceptionMessages = exceptionMessages.ToList(),
            ExceptionStackTrace = exceptionStackTrace
        };
        messages.OnNext(new(new(result)));
    }

    bool ShouldReducerBeActive(Type reducerType)
    {
        var active = reducerType.IsActive();
        if (!active)
        {
            return false;
        }

        var readModelType = reducerType.GetReadModelType();
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
