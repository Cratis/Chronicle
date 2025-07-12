// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Contracts.Sinks;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Sinks;
using Cratis.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducers"/>.
/// </summary>
public class Reducers : IReducers
{
#if NET9_0
    static readonly Lock _registerLock = new();
#else
    static readonly object _registerLock = new();
#endif
    readonly IChronicleServicesAccessor _servicesAccessor;
    readonly IEventStore _eventStore;
    readonly IClientArtifactsProvider _clientArtifacts;
    readonly IServiceProvider _serviceProvider;
    readonly IReducerValidator _reducerValidator;
    readonly IEventTypes _eventTypes;
    readonly IEventSerializer _eventSerializer;
    readonly IModelNameResolver _modelNameResolver;
    readonly IJsonSchemaGenerator _jsonSchemaGenerator;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly ILogger<Reducers> _logger;
    IEnumerable<Type> _aggregateRootStateTypes = [];
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
    /// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing of events.</param>
    /// <param name="modelNameResolver"><see cref="IModelNameResolver"/> for resolving read model names.</param>
    /// <param name="jsonSchemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for JSON serialization.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public Reducers(
        IEventStore eventStore,
        IClientArtifactsProvider clientArtifacts,
        IServiceProvider serviceProvider,
        IReducerValidator reducerValidator,
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        IModelNameResolver modelNameResolver,
        IJsonSchemaGenerator jsonSchemaGenerator,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<Reducers> logger)
    {
        eventStore.Connection.Lifecycle.OnConnected += Register;
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
        _eventSerializer = eventSerializer;
        _modelNameResolver = modelNameResolver;
        _jsonSchemaGenerator = jsonSchemaGenerator;
        _jsonSerializerOptions = jsonSerializerOptions;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task Discover()
    {
        _aggregateRootStateTypes = _clientArtifacts.AggregateRootStateTypes;
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
    public Task<IReducerHandler> Register<TReducer, TModel>()
        where TReducer : IReducerFor<TModel>
        where TModel : class
    {
        var reducerType = typeof(TReducer);
        var modelType = typeof(TModel);
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
    public IReducerHandler GetHandlerForReadModelType(Type modelType) => _handlersByModelType[modelType];

    /// <inheritdoc/>
    public bool HasReducerFor(Type modelType) => _handlersByModelType.ContainsKey(modelType);

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

    ReducerHandler CreateHandlerFor(Type reducerType, Type modelType)
    {
        var handler = new ReducerHandler(
            _eventStore,
            reducerType.GetReducerId(),
            reducerType.GetEventSequenceId(),
            new ReducerInvoker(
                _eventTypes,
                reducerType,
                modelType),
            _eventSerializer,
            ShouldReducerBeActive(reducerType, modelType));

        CancellationTokenRegistration? register = null;
        register = handler.CancellationToken.Register(() =>
        {
            _handlersByType.Remove(reducerType);
            _handlersByModelType.Remove(modelType);
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
                EventTypes = handler.EventTypes.Select(et => new EventTypeWithKeyExpression { EventType = et.ToContract(), Key = "$eventSourceId" }).ToArray(),
                Model = new Contracts.Models.ModelDefinition
                {
                    Name = _modelNameResolver.GetNameFor(handler.ReadModelType),
                    Schema = _jsonSchemaGenerator.Generate(handler.ReadModelType).ToJson()
                },
                Sink = new SinkDefinition
                {
                    TypeId = WellKnownSinkTypes.MongoDB
                }
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
            var metadata = @event.Metadata.ToClient();
            var context = @event.Context.ToClient();
            var contentAsExpando = JsonSerializer.Deserialize<ExpandoObject>(@event.Content)!;
            return new AppendedEvent(
                metadata,
                context,
                contentAsExpando);
        }).ToList();

        try
        {
            await using var serviceProviderScope = _serviceProvider.CreateAsyncScope();
            BaseIdentityProvider.SetCurrentIdentity(Identity.System);
            var initialState = operation.InitialState is null ? null : JsonSerializer.Deserialize(operation.InitialState, handler.ReadModelType, _jsonSerializerOptions);
            var reduceResult = await handler.OnNext(appendedEvents, initialState, serviceProviderScope.ServiceProvider);

            lastSuccessfullyObservedEvent = reduceResult.LastSuccessfullyObservedEvent;
            if (reduceResult.IsSuccess)
            {
                modelState = JsonSerializer.Serialize(reduceResult.ModelState, _jsonSerializerOptions);
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
            ModelState = modelState,
            State = state,
            LastSuccessfulObservation = lastSuccessfullyObservedEvent,
            ExceptionMessages = exceptionMessages.ToList(),
            ExceptionStackTrace = exceptionStackTrace
        };
        messages.OnNext(new(new(result)));
    }

    bool ShouldReducerBeActive(Type reducerType, Type readModelType)
    {
        var active = reducerType.IsActive();
        if (!active || _aggregateRootStateTypes.Contains(readModelType))
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
