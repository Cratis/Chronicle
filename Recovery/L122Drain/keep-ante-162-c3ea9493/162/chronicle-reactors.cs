using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Primitives;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Registrations;
using Cratis.Concepts;
using Cratis.Monads;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneOf;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents an implementation of <see cref="T:Cratis.Chronicle.Reactors.IReactors" />.
/// </summary>
public class Reactors : IReactors, IReactorPartitionRecovery
{
	private sealed record ReactorRegistration(ReactorHandler Handler, bool IsReplayable, string[] Tags, string[] FilterTags, EventSourceType EventSourceType, EventStreamType EventStreamType, Func<ReactorEvent, CancellationToken, Task>? Handle, bool StartAtTail = false)
	{
		/// <summary>
		/// Gets or sets whether the subscription has been sent on the current connection.
		/// </summary>
		public bool IsRegistered { get; set; }
	}

	private static readonly Lock _registerLock = new Lock();

	private readonly IEventStore _eventStore;

	private readonly IEventTypes _eventTypes;

	private readonly IClientArtifactsProvider _clientArtifactsProvider;

	private readonly IServiceProvider _serviceProvider;

	private readonly IClientArtifactsActivator _artifactActivator;

	private readonly IActivateReactorMiddlewares _middlewaresActivator;

	private readonly IEventSerializer _eventSerializer;

	private readonly ICausationManager _causationManager;

	private readonly IIdentityProvider _identityProvider;

	private readonly IActivitySource<Reactors> _activitySource;

	private readonly IReactorSideEffectHandlers _sideEffectHandlers;

	private readonly IReactorContextValuesBuilder _reactorContextValuesBuilder;

	private readonly IReactorMethodArgumentsResolver _argumentsResolver;

	private readonly ILogger<Reactors> _logger;

	private readonly ILoggerFactory _loggerFactory;

	private readonly IChronicleServicesAccessor _servicesAccessor;

	private readonly List<(ReactorId Id, Action<IReactorBuilder> Define)> _pendingDefinitions = new List<(ReactorId, Action<IReactorBuilder>)>();

	private IReadOnlyDictionary<ReactorId, ReactorRegistration> _handlers = FrozenDictionary<ReactorId, ReactorRegistration>.Empty;

	private bool _registered;

	/// <summary>
	/// Gets the generation delivery policy supplied by the event store.
	/// </summary>
	internal Cratis.Chronicle.Observation.EventGenerationDelivery GenerationDelivery { get; init; }

	/// <summary>
	/// Gets how many partitions of one reactor are handled at the same time.
	/// </summary>
	internal int MaxConcurrentPartitions { get; init; } = PartitionedDeliveryScheduler.DefaultMaxConcurrentPartitions;

	/// <summary>
	/// Gets the current discovered and runtime reactor handlers.
	/// </summary>
	internal IEnumerable<IReactorHandler> Handlers => _handlers.Values.Select((ReactorRegistration _) => _.Handler);

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Cratis.Chronicle.Reactors.Reactors" /> class.
	/// </summary>
	/// <param name="eventStore"><see cref="T:Cratis.Chronicle.IEventStore" /> the Reactors belong to.</param>
	/// <param name="eventTypes"><see cref="T:Cratis.Chronicle.Events.IEventTypes" /> for resolving event types.</param>
	/// <param name="clientArtifactsProvider"><see cref="T:Cratis.Chronicle.IClientArtifactsProvider" /> for getting client artifacts.</param>
	/// <param name="serviceProvider"><see cref="T:System.IServiceProvider" /> to get instances of types.</param>
	/// <param name="artifactActivator"><see cref="T:Cratis.Chronicle.IClientArtifactsActivator" /> for creating artifact instances.</param>
	/// <param name="middlewaresActivator"><see cref="T:Cratis.Chronicle.Reactors.IReactorMiddlewares" /> to call.</param>
	/// <param name="eventSerializer"><see cref="T:Cratis.Chronicle.Events.IEventSerializer" /> for serializing of events.</param>
	/// <param name="causationManager"><see cref="T:Cratis.Chronicle.Auditing.ICausationManager" /> for working with causation.</param>
	/// <param name="identityProvider"><see cref="T:Cratis.Chronicle.Identities.IIdentityProvider" /> for managing identity context.</param>
	/// <param name="activitySource"><see cref="T:Cratis.Traces.IActivitySource`1" /> for tracing reactor event handling.</param>
	/// <param name="sideEffectHandlers"><see cref="T:Cratis.Chronicle.Reactors.SideEffects.IReactorSideEffectHandlers" /> for processing return values as side effects.</param>
	/// <param name="reactorContextValuesBuilder"><see cref="T:Cratis.Chronicle.Reactors.SideEffects.IReactorContextValuesBuilder" /> for resolving append-metadata for side-effect events.</param>
	/// <param name="argumentsResolver"><see cref="T:Cratis.Chronicle.Reactors.IReactorMethodArgumentsResolver" /> for resolving handler method arguments.</param>
	/// <param name="logger"><see cref="T:Microsoft.Extensions.Logging.ILogger" /> for logging.</param>
	/// <param name="loggerFactory"><see cref="T:Microsoft.Extensions.Logging.ILoggerFactory" /> for creating loggers.</param>
	public Reactors(IEventStore eventStore, IEventTypes eventTypes, IClientArtifactsProvider clientArtifactsProvider, IServiceProvider serviceProvider, IClientArtifactsActivator artifactActivator, IActivateReactorMiddlewares middlewaresActivator, IEventSerializer eventSerializer, ICausationManager causationManager, IIdentityProvider identityProvider, IActivitySource<Reactors> activitySource, IReactorSideEffectHandlers sideEffectHandlers, IReactorContextValuesBuilder reactorContextValuesBuilder, IReactorMethodArgumentsResolver argumentsResolver, ILogger<Reactors> logger, ILoggerFactory loggerFactory)
	{
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Expected O, but got Unknown
		_eventStore = eventStore;
		_servicesAccessor = eventStore.Connection as IChronicleServicesAccessor;
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
		_eventStore.Connection.Lifecycle.OnDisconnected += (Disconnected)delegate
		{
			using (_registerLock.EnterScope())
			{
				_registered = false;
				RecreateHandlersForReconnect();
			}
			return Task.CompletedTask;
		};
	}

	/// <inheritdoc />
	public Task Discover()
	{
		_logger.DiscoverAllReactors();
		using (_registerLock.EnterScope())
		{
			Type[] source = _clientArtifactsProvider.Reactors.ToArray();
			ReactorRegistration[] source2 = _handlers.Values.Where((ReactorRegistration _) => _.Handle != null).ToArray();
			IReactorDefinition[] source3 = _pendingDefinitions.Select<(ReactorId, Action<IReactorBuilder>), IReactorDefinition>(((ReactorId Id, Action<IReactorBuilder> Define) _) => Define(_.Id, _.Define)).ToArray();
			IEnumerable<ReactorId> source4 = source2.Select((ReactorRegistration _) => _.Handler.Id).Concat(source3.Select((IReactorDefinition _) => _.Id)).Concat(source.Select(ResolveId));
			IGrouping<ReactorId, ReactorId> duplicate = (from _ in source4
				group _ by _).FirstOrDefault((IGrouping<ReactorId, ReactorId> _) => _.Count() > 1);
			if (duplicate != null)
			{
				Type[] array = source.Where((Type _) => ResolveId(_) == duplicate.Key).Take(2).ToArray();
				if (array.Length == 2)
				{
					throw new ReactorAlreadyRegistered(duplicate.Key, array[0], array[1]);
				}
				throw new ReactorAlreadyRegistered(duplicate.Key);
			}
			ReactorRegistration[] first = source.Select(CreateRegistrationFor).ToArray();
			DisconnectHandlers();
			_registered = false;
			_handlers = first.Concat(source2.Select(RecreateRegistration)).Concat(source3.Select(CreateRegistrationFor)).ToFrozenDictionary((ReactorRegistration _) => _.Handler.Id);
			_pendingDefinitions.Clear();
		}
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task Register()
	{
		if (_registered)
		{
			return Task.CompletedTask;
		}
		using (_registerLock.EnterScope())
		{
			if (_registered)
			{
				return Task.CompletedTask;
			}
			foreach (ReactorRegistration item in _handlers.Values.Where((ReactorRegistration _) => !_.IsRegistered))
			{
				RegisterReactor(item);
				item.IsRegistered = true;
			}
			_registered = true;
		}
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task<IReactorHandler> Register<TReactor>() where TReactor : IReactor
	{
		using (_registerLock.EnterScope())
		{
			ReactorRegistration reactorRegistration = CreateRegistrationFor(typeof(TReactor));
			AddRegistration(reactorRegistration);
			return Task.FromResult((IReactorHandler)reactorRegistration.Handler);
		}
	}

	/// <inheritdoc />
	public Task<IReactorHandler> Register(ReactorId id, Action<IReactorDefinitionBuilder> configure, Func<ReactorEvent, CancellationToken, Task> handle)
	{
		ArgumentNullException.ThrowIfNull(configure, "configure");
		ArgumentNullException.ThrowIfNull(handle, "handle");
		ReactorDefinitionBuilder reactorDefinitionBuilder = new ReactorDefinitionBuilder();
		configure(reactorDefinitionBuilder);
		(EventSequenceId, Cratis.Chronicle.Events.EventType[], bool) tuple = reactorDefinitionBuilder.Build(id);
		using (_registerLock.EnterScope())
		{
			ReactorHandler reactorHandler = CreateHandler(id, typeof(object), tuple.Item1, tuple.Item2);
			ReactorRegistration registration = new ReactorRegistration(reactorHandler, tuple.Item3, Array.Empty<string>(), Array.Empty<string>(), EventSourceType.Unspecified, EventStreamType.All, handle);
			AddRegistration(registration);
			return Task.FromResult((IReactorHandler)reactorHandler);
		}
	}

	/// <inheritdoc />
	public IReactorDefinition Define(ReactorId id, Action<IReactorBuilder> define)
	{
		ArgumentNullException.ThrowIfNull(define, "define");
		ReactorBuilder reactorBuilder = new ReactorBuilder(_eventTypes);
		define(reactorBuilder);
		return reactorBuilder.Build(id);
	}

	/// <inheritdoc />
	public Task<IReactorHandler> Register(ReactorId id, Action<IReactorBuilder> define)
	{
		return Register(Define(id, define));
	}

	/// <inheritdoc />
	public Task<IReactorHandler> Register(IReactorDefinition definition)
	{
		ArgumentNullException.ThrowIfNull(definition, "definition");
		using (_registerLock.EnterScope())
		{
			ReactorRegistration reactorRegistration = CreateRegistrationFor(definition);
			AddRegistration(reactorRegistration);
			return Task.FromResult((IReactorHandler)reactorRegistration.Handler);
		}
	}

	/// <inheritdoc />
	public void Unregister(ReactorId id)
	{
		using (_registerLock.EnterScope())
		{
			if (_handlers.TryGetValue(id, out ReactorRegistration value))
			{
				_handlers = _handlers.Where<KeyValuePair<ReactorId, ReactorRegistration>>((KeyValuePair<ReactorId, ReactorRegistration> _) => _.Key != id).ToFrozenDictionary();
				value.Handler.Disconnect();
				((IDisposable)value.Handler)?.Dispose();
			}
		}
	}

	/// <inheritdoc />
	public IReactorHandler GetHandlerFor<TReactor>() where TReactor : IReactor
	{
		return GetHandlerFor(typeof(TReactor));
	}

	/// <inheritdoc />
	public IReactorHandler GetHandlerById(ReactorId id)
	{
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		if (_handlers.TryGetValue(id, out ReactorRegistration value))
		{
			return value.Handler;
		}
		HasReactorRequest request = new HasReactorRequest
		{
			EventStore = _eventStore.Name,
			Namespace = ConceptAs<string>.op_Implicit((ConceptAs<string>)(object)_eventStore.Namespace),
			ReactorId = ((ConceptAs<string>)(object)id).Value
		};
		HasReactorResponse result = _servicesAccessor.Services.Reactors.HasReactor(request).GetAwaiter().GetResult();
		if (!result.Exists)
		{
			ThrowIfUnknownReactorId(null, id);
		}
		return new ReactorHandler(_eventStore, id, typeof(object), new EventSequenceId(result.EventSequenceId), Array.Empty<Cratis.Chronicle.Events.EventType>(), _causationManager, _identityProvider);
	}

	/// <inheritdoc />
	public Task<IEnumerable<Cratis.Chronicle.Observation.FailedPartition>> GetFailedPartitionsFor<TReactor>()
	{
		return GetFailedPartitionsFor(typeof(TReactor));
	}

	/// <inheritdoc />
	public Task<IEnumerable<Cratis.Chronicle.Observation.FailedPartition>> GetFailedPartitionsFor(Type reactorType)
	{
		return GetHandlerFor(reactorType).GetFailedPartitions();
	}

	/// <inheritdoc />
	public async Task<ReactorPartitionRetryOutcome> RetryFailedPartitionFor(Type reactorType, Partition partition)
	{
		IReactorHandler handlerFor = GetHandlerFor(reactorType);
		return (await _servicesAccessor.Services.Observers.RetryPartition(new RetryPartition
		{
			EventStore = _eventStore.Name,
			Namespace = ConceptAs<string>.op_Implicit((ConceptAs<string>)(object)_eventStore.Namespace),
			ObserverId = ConceptAs<string>.op_Implicit((ConceptAs<string>)(object)handlerFor.Id),
			EventSequenceId = ConceptAs<string>.op_Implicit((ConceptAs<string>)(object)handlerFor.EventSequenceId),
			Partition = ConceptAs<string>.op_Implicit((ConceptAs<string>)(object)partition)
		})).Outcome.ToClient();
	}

	/// <inheritdoc />
	public async Task<ReactorPartitionQuarantineClearResult> ClearFailedPartitionQuarantineFor(Type reactorType, Partition partition, bool retryImmediately = true)
	{
		IReactorHandler handlerFor = GetHandlerFor(reactorType);
		return (await _servicesAccessor.Services.Observers.ClearPartitionQuarantine(new ClearPartitionQuarantine
		{
			EventStore = _eventStore.Name,
			Namespace = ConceptAs<string>.op_Implicit((ConceptAs<string>)(object)_eventStore.Namespace),
			ObserverId = ConceptAs<string>.op_Implicit((ConceptAs<string>)(object)handlerFor.Id),
			EventSequenceId = ConceptAs<string>.op_Implicit((ConceptAs<string>)(object)handlerFor.EventSequenceId),
			Partition = ConceptAs<string>.op_Implicit((ConceptAs<string>)(object)partition),
			RetryImmediately = retryImmediately
		})).ToClient();
	}

	/// <inheritdoc />
	public Task<ReactorState> GetStateFor<TReactor>() where TReactor : IReactor
	{
		Type typeFromHandle = typeof(TReactor);
		return GetHandlerFor(typeFromHandle).GetState();
	}

	/// <inheritdoc />
	public Task<JobId> Replay<TReactor>() where TReactor : IReactor
	{
		Type typeFromHandle = typeof(TReactor);
		IReactorHandler handlerFor = GetHandlerFor(typeFromHandle);
		return Replay(handlerFor.Id);
	}

	/// <inheritdoc />
	public async Task<JobId> Replay(ReactorId reactorId)
	{
		Guid result;
		return Guid.TryParse((await _servicesAccessor.Services.Observers.Replay(new Replay
		{
			EventStore = _eventStore.Name,
			Namespace = ConceptAs<string>.op_Implicit((ConceptAs<string>)(object)_eventStore.Namespace),
			ObserverId = ConceptAs<string>.op_Implicit((ConceptAs<string>)(object)reactorId),
			EventSequenceId = string.Empty
		})).JobId, out result) ? new JobId(result) : JobId.NotSet;
	}

	/// <inheritdoc />
	public Task<FastForwardResult> FastForward<TReactor>() where TReactor : IReactor
	{
		return FastForward(GetHandlerFor<TReactor>().Id);
	}

	/// <inheritdoc />
	public Task<FastForwardResult> FastForward(ReactorId reactorId)
	{
		return ObserverFastForwarder.FastForward(_servicesAccessor.Services.Observers, _eventStore.Name, ConceptAs<string>.op_Implicit((ConceptAs<string>)(object)_eventStore.Namespace), ConceptAs<string>.op_Implicit((ConceptAs<string>)(object)reactorId));
	}

	/// <summary>
	/// Add a fluent reactor definition that is built and registered by the next <see cref="M:Cratis.Chronicle.Reactors.Reactors.Discover" />.
	/// </summary>
	/// <param name="id">The stable reactor identifier.</param>
	/// <param name="define">Declares the reactor on an <see cref="T:Cratis.Chronicle.Reactors.IReactorBuilder" />.</param>
	/// <remarks>
	/// This is how a reactor registered while the client is being configured reaches the event store: at that point
	/// the event types it refers to are not known yet, so defining it is left to discovery, which knows them.
	/// </remarks>
	internal void Add(ReactorId id, Action<IReactorBuilder> define)
	{
		using (_registerLock.EnterScope())
		{
			_pendingDefinitions.Add((id, define));
		}
	}

	private static void ThrowIfUnknownReactorId(IReactorHandler? handler, ReactorId reactorId)
	{
		if (handler == null)
		{
			throw new UnknownReactorId(reactorId);
		}
	}

	private ReactorRegistration CreateRegistrationFor(Type reactorType)
	{
		IImmutableList<Cratis.Chronicle.Events.EventType> eventTypesFor = ReactorInvoker.GetEventTypesFor(_eventStore.EventTypes, reactorType, _sideEffectHandlers);
		ObserverHasOnlyGenerationHandlers.ThrowIfEmpty(reactorType, eventTypesFor);
		ReactorHandler handler = CreateHandler(ResolveId(reactorType), reactorType, reactorType.GetEventSequenceId(_eventStore.EventTypes, _eventStore.Name, _sideEffectHandlers), eventTypesFor);
		(EventSourceType, EventStreamType) eventSourceFilter = reactorType.GetEventSourceFilter(_eventStore.EventSources);
		return new ReactorRegistration(handler, !reactorType.IsDefined(typeof(OnceOnlyAttribute), inherit: false), reactorType.GetTags().ToArray(), reactorType.GetFilterTags().ToArray(), eventSourceFilter.Item1, eventSourceFilter.Item2, null, reactorType.IsDefined(typeof(StartAtTailAttribute), inherit: false));
	}

	private ReactorRegistration CreateRegistrationFor(IReactorDefinition definition)
	{
		ReactorId id = definition.Id;
		Type? typeFromHandle = typeof(object);
		EventSequenceId eventSequenceId = definition.EventSequenceId;
		List<Cratis.Chronicle.Events.EventType> list = new List<Cratis.Chronicle.Events.EventType>();
		list.AddRange(definition.EventTypes);
		return new ReactorRegistration(CreateHandler(id, typeFromHandle, eventSequenceId, new global::<>z__ReadOnlyList<Cratis.Chronicle.Events.EventType>(list)), definition.IsReplayable, Array.Empty<string>(), Array.Empty<string>(), EventSourceType.Unspecified, EventStreamType.All, CreateDispatcherFor(definition));
	}

	/// <summary>
	/// Create the callback that turns a delivered event into the object a fluent definition's handlers receive.
	/// </summary>
	/// <param name="definition">The <see cref="T:Cratis.Chronicle.Reactors.IReactorDefinition" /> to dispatch to.</param>
	/// <returns>The callback.</returns>
	/// <remarks>
	/// The event is deserialized in the generation the definition subscribed to, as it is for a discovered reactor, so
	/// a handler always receives the CLR type it was declared for even when the event was appended in another
	/// generation.
	/// </remarks>
	private Func<ReactorEvent, CancellationToken, Task> CreateDispatcherFor(IReactorDefinition definition)
	{
		Dictionary<EventTypeId, EventTypeGeneration> generations = definition.EventTypes.DistinctBy((Cratis.Chronicle.Events.EventType _) => _.Id).ToDictionary((Cratis.Chronicle.Events.EventType _) => _.Id, (Cratis.Chronicle.Events.EventType _) => _.Generation);
		return async delegate(ReactorEvent delivered, CancellationToken _)
		{
			Cratis.Chronicle.Events.EventContext context = delivered.Context;
			JsonObject json = delivered.Content;
			EventTypeGeneration value;
			EventTypeGeneration eventTypeGeneration = (generations.TryGetValue(context.EventType.Id, out value) ? value : context.EventType.Generation);
			if (eventTypeGeneration != context.EventType.Generation && delivered.GenerationalContent.TryGetValue((int)((ConceptAs<uint>)eventTypeGeneration).Value, out string value2))
			{
				json = JsonNode.Parse(value2).AsObject();
				context = context with
				{
					EventType = context.EventType with
					{
						Generation = eventTypeGeneration
					}
				};
			}
			Type clrTypeFor = _eventTypes.GetClrTypeFor(context.EventType.Id, context.EventType.Generation);
			object obj = await _eventSerializer.Deserialize(clrTypeFor, json);
			await definition.Handle(obj, context);
		};
	}

	private ReactorHandler CreateHandler(ReactorId id, Type reactorType, EventSequenceId sequenceId, IEnumerable<Cratis.Chronicle.Events.EventType> eventTypes)
	{
		ReactorHandler handler = new ReactorHandler(_eventStore, id, reactorType, sequenceId, eventTypes, _causationManager, _identityProvider);
		handler.CancellationToken.Register(delegate
		{
			using (_registerLock.EnterScope())
			{
				if (_handlers.TryGetValue(id, out ReactorRegistration value) && value.Handler == handler)
				{
					_handlers = _handlers.Where<KeyValuePair<ReactorId, ReactorRegistration>>((KeyValuePair<ReactorId, ReactorRegistration> _) => _.Key != id).ToFrozenDictionary();
				}
			}
		});
		return handler;
	}

	private void AddRegistration(ReactorRegistration registration)
	{
		if (_handlers.ContainsKey(registration.Handler.Id))
		{
			registration.Handler.Disconnect();
			((IDisposable)registration.Handler)?.Dispose();
			throw new ReactorAlreadyRegistered(registration.Handler.Id);
		}
		try
		{
			RegisterReactor(registration);
			registration.IsRegistered = true;
			_handlers = _handlers.Append(new KeyValuePair<ReactorId, ReactorRegistration>(registration.Handler.Id, registration)).ToFrozenDictionary();
		}
		catch
		{
			registration.Handler.Disconnect();
			registration.Handler.Dispose();
			throw;
		}
	}

	private void DisconnectHandlers()
	{
		foreach (ReactorRegistration item in _handlers.Values.ToList())
		{
			item.Handler.Disconnect();
			((IDisposable)item.Handler)?.Dispose();
		}
	}

	private void RecreateHandlersForReconnect()
	{
		ReactorRegistration[] source = _handlers.Values.ToArray();
		DisconnectHandlers();
		_handlers = source.Select(RecreateRegistration).ToFrozenDictionary((ReactorRegistration _) => _.Handler.Id);
	}

	private ReactorRegistration RecreateRegistration(ReactorRegistration registration)
	{
		ReactorHandler handler = registration.Handler;
		return registration with
		{
			Handler = CreateHandler(handler.Id, handler.ReactorType, handler.EventSequenceId, handler.EventTypes),
			IsRegistered = false
		};
	}

	private void RegisterReactor(ReactorRegistration registration)
	{
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		ReactorHandler handler = registration.Handler;
		if (GenerationDelivery == Cratis.Chronicle.Observation.EventGenerationDelivery.Pinned)
		{
			GenerationDeliveryValidation.Validate(handler.EventTypes);
		}
		_logger.RegisteringReactor(handler.Id);
		RegisterReactor value = new RegisterReactor
		{
			ConnectionId = ConnectionId.op_Implicit(_eventStore.Connection.Lifecycle.ConnectionId),
			EventStore = _eventStore.Name,
			Namespace = ConceptAs<string>.op_Implicit((ConceptAs<string>)(object)_eventStore.Namespace),
			Reactor = new ReactorDefinition
			{
				GenerationDelivery = (Cratis.Chronicle.Contracts.Observation.EventGenerationDelivery)GenerationDelivery,
				ReactorId = ConceptAs<string>.op_Implicit((ConceptAs<string>)(object)handler.Id),
				EventSequenceId = ConceptAs<string>.op_Implicit((ConceptAs<string>)(object)handler.EventSequenceId),
				EventTypes = handler.EventTypes.Select((Cratis.Chronicle.Events.EventType et) => new EventTypeWithKeyExpression
				{
					EventType = et.ToContract(),
					Key = "$eventSourceId"
				}).ToArray(),
				IsReplayable = registration.IsReplayable,
				IsNotReplayable = !registration.IsReplayable,
				StartAtTail = registration.StartAtTail,
				Tags = registration.Tags,
				Filters = new ObserverFilters
				{
					FilterTags = registration.FilterTags,
					EventSourceType = ((ConceptAs<string>)(object)registration.EventSourceType).Value,
					EventStreamType = ((ConceptAs<string>)(object)registration.EventStreamType).Value
				}
			}
		};
		SerializedMessages<ReactorMessage> messages = new SerializedMessages<ReactorMessage>(new ReactorMessage(new OneOf<RegisterReactor, ReactorResult>(value)));
		CancellationToken cancellationToken = handler.CancellationToken;
		PartitionedDeliveryScheduler scheduler = new PartitionedDeliveryScheduler(MaxConcurrentPartitions);
		ObservableExtensions.Subscribe<Unit>(Observable.Merge<Unit>(Observable.Select<EventsToObserve, IObservable<Unit>>(_servicesAccessor.Services.Reactors.Observe(messages.Messages, CallContext.op_Implicit(cancellationToken)), (Func<EventsToObserve, IObservable<Unit>>)delegate(EventsToObserve events)
		{
			ReplayState replayState = events.ReplayState;
			bool flag = (uint)(replayState - 1) <= 1u;
			return TaskObservableExtensions.ToObservable(flag ? scheduler.ScheduleBarrier(Deliver) : scheduler.Schedule(events.Partition ?? string.Empty, Deliver));
			async Task Deliver()
			{
				await ObserverMethod(messages, registration, events, cancellationToken);
				_logger.EventHandlingCompleted(handler.Id);
			}
		})), (Action<Unit>)delegate
		{
		}, (Action<Exception>)delegate(Exception ex)
		{
			scheduler.Stop();
			switch (cancellationToken.IsCancellationRequested ? KernelConnectionErrorKind.Cancelled : ex.ClassifyStreamError())
			{
			case KernelConnectionErrorKind.Cancelled:
				_logger.RegisteringReactorStreamCancelled(handler.Id, ex);
				messages.Dispose();
				scheduler.DrainAndDispose();
				break;
			case KernelConnectionErrorKind.ConnectionLost:
				_logger.ReactorStreamLostConnection(handler.Id, ex);
				messages.Dispose();
				ScheduleReconnect();
				break;
			case KernelConnectionErrorKind.SessionLost:
				_logger.ReactorStreamLostConnection(handler.Id, ex);
				messages.Dispose();
				scheduler.DrainAndDispose();
				_eventStore.Connection.RequestReconnect();
				break;
			default:
			{
				ReactorObservationStreamFailed exception = new ReactorObservationStreamFailed(handler.Id, ex);
				_logger.RegisteringReactorFailed(handler.Id, exception);
				messages.Dispose();
				ScheduleReconnect();
				break;
			}
			}
		}, (Action)delegate
		{
			scheduler.Stop();
			_logger.RegisteringReactorStreamCompleted(handler.Id);
			messages.Dispose();
			ScheduleReconnect();
		});
		void ScheduleReconnect()
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				Task.Run(async delegate
				{
					_ = 1;
					try
					{
						await scheduler.DrainAndDispose();
						await Task.Delay(TimeSpan.FromSeconds(2L), cancellationToken);
						_logger.ReconnectingReactor(handler.Id);
						RegisterReactor(registration);
					}
					catch (OperationCanceledException exception)
					{
						_logger.RegisteringReactorStreamCancelled(handler.Id, exception);
					}
				});
			}
		}
	}

	private async Task ObserverMethod(SerializedMessages<ReactorMessage> messages, ReactorRegistration registration, EventsToObserve events, CancellationToken cancellationToken)
	{
		ReactorHandler handler = registration.Handler;
		if (events.ReplayState != ReplayState.None)
		{
			if (registration.Handle == null)
			{
				await HandleReplayNotification(handler, events.ReplayState, events.Partition);
			}
			return;
		}
		IActivityScope<Reactors> span = _activitySource.Handle(_eventStore.Name, ConceptAs<string>.op_Implicit((ConceptAs<string>)(object)_eventStore.Namespace), ConceptAs<string>.op_Implicit((ConceptAs<string>)(object)handler.EventSequenceId), ConceptAs<string>.op_Implicit((ConceptAs<string>)(object)handler.Id));
		EventSequenceNumber lastSuccessfullyObservedEvent;
		IEnumerable<string> exceptionMessages;
		string exceptionStackTrace;
		ObservationState state;
		try
		{
			lastSuccessfullyObservedEvent = EventSequenceNumber.Unavailable;
			exceptionMessages = Enumerable.Empty<string>();
			exceptionStackTrace = string.Empty;
			state = ObservationState.Success;
			if (registration.Handle != null)
			{
				foreach (Cratis.Chronicle.Contracts.Events.AppendedEvent @event in events.Events)
				{
					if (cancellationToken.IsCancellationRequested)
					{
						return;
					}
					_logger.EventReceived(@event.Context.EventType.Id, handler.Id);
					try
					{
						ReactorEvent reactorEvent = new ReactorEvent(@event.Context.ToClient(), JsonNode.Parse(@event.Content).AsObject(), new Dictionary<int, string>(@event.GenerationalContent));
						using (handler.BeginHandlingScope(reactorEvent.Context))
						{
							_ = 1;
							try
							{
								await registration.Handle(reactorEvent, cancellationToken);
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
					catch (Exception ex2)
					{
						FailedToHandleEventWithException(ex2, @event.Context.EventType.Id);
						break;
					}
				}
				if (!cancellationToken.IsCancellationRequested)
				{
					PublishResult();
				}
				return;
			}
			await using AsyncServiceScope serviceProviderScope = _serviceProvider.CreateAsyncScope();
			Catch<ActivatedArtifact> val = _artifactActivator.Activate(serviceProviderScope.ServiceProvider, handler.ReactorType);
			Exception ex3 = default(Exception);
			if (val.TryGetException(ref ex3))
			{
				FailedActivatingReactor(ex3);
				PublishResult();
				return;
			}
			await using ActivatedArtifact activatedReactor = ((OneOfBase<ActivatedArtifact, Exception>)(object)val).AsT0;
			await using IReactorMiddlewares middlewares = _middlewaresActivator.Activate(serviceProviderScope.ServiceProvider);
			ReactorInvoker reactorInvoker = new ReactorInvoker(_eventTypes, middlewares, handler.ReactorType, activatedReactor, _loggerFactory.CreateLogger<ReactorInvoker>(), _sideEffectHandlers, _eventStore, _reactorContextValuesBuilder, _argumentsResolver, serviceProviderScope.ServiceProvider)
			{
				StoppingToken = cancellationToken,
				RegisteredId = handler.Id,
				RegisteredEventSequenceId = handler.EventSequenceId
			};
			foreach (Cratis.Chronicle.Contracts.Events.AppendedEvent @event in events.Events)
			{
				_logger.EventReceived(@event.Context.EventType.Id, handler.Id);
				try
				{
					Cratis.Chronicle.Events.EventContext context = @event.Context.ToClient();
					EventTypeGeneration eventTypeGeneration = handler.EventTypes.FirstOrDefault((Cratis.Chronicle.Events.EventType et) => et.Id == context.EventType.Id)?.Generation ?? context.EventType.Generation;
					Type clrTypeFor = _eventTypes.GetClrTypeFor(context.EventType.Id, eventTypeGeneration);
					string json;
					if (eventTypeGeneration != context.EventType.Generation && @event.GenerationalContent.TryGetValue((int)((ConceptAs<uint>)eventTypeGeneration).Value, out string value))
					{
						json = value;
						context = context with
						{
							EventType = context.EventType with
							{
								Generation = eventTypeGeneration
							}
						};
					}
					else
					{
						json = @event.Content;
					}
					object content = await _eventSerializer.Deserialize(clrTypeFor, JsonNode.Parse(json).AsObject());
					ReactorInvocationResult reactorInvocationResult = await handler.OnNext(context, content, reactorInvoker);
					if (reactorInvocationResult.IsFailed)
					{
						FailedToHandleEvent(reactorInvocationResult, @event.Context.EventType.Id);
						break;
					}
					lastSuccessfullyObservedEvent = @event.Context.SequenceNumber;
				}
				catch (Exception ex4)
				{
					FailedToHandleEventWithException(ex4, @event.Context.EventType.Id);
					break;
				}
			}
			PublishResult();
		}
		finally
		{
			((IDisposable)span)?.Dispose();
		}
		void FailedActivatingReactor(Exception ex5)
		{
			exceptionMessages = ExceptionExtensions.GetAllMessages(ex5);
			exceptionStackTrace = ex5.StackTrace ?? string.Empty;
			state = ObservationState.Failed;
		}
		void FailedToHandleEvent(ReactorInvocationResult invocationResult, EventTypeId eventTypeId)
		{
			(IEnumerable<string>, string) failureDetails = invocationResult.GetFailureDetails();
			exceptionMessages = failureDetails.Item1.ToArray();
			exceptionStackTrace = failureDetails.Item2;
			state = ObservationState.Failed;
			Exception ex5 = default(Exception);
			if (invocationResult.ExceptionResult.TryGetException(ref ex5))
			{
				LogErrorWhileHandlingEvent(ex5, eventTypeId);
			}
			else
			{
				string targetEventSourceIds = (((object)invocationResult.SideEffectFailure != null) ? string.Join(", ", invocationResult.SideEffectFailure.GetTargetEventSourceIds()) : string.Empty);
				_logger.ReactorSideEffectAppendFailed(eventTypeId, handler.Id, targetEventSourceIds, string.Join(Environment.NewLine, exceptionMessages));
			}
		}
		void FailedToHandleEventWithException(Exception ex5, EventTypeId eventTypeId)
		{
			LogErrorWhileHandlingEvent(ex5, eventTypeId);
			exceptionMessages = ExceptionExtensions.GetAllMessages(ex5);
			exceptionStackTrace = ex5.StackTrace ?? string.Empty;
			state = ObservationState.Failed;
		}
		void LogErrorWhileHandlingEvent(Exception ex5, EventTypeId eventTypeId)
		{
			if (ex5.IsInterruptedByShutdown(cancellationToken))
			{
				_logger.HandlingEventInterruptedByShutdown(ex5, eventTypeId, handler.Id);
			}
			else
			{
				_logger.ErrorWhileHandlingEvent(ex5, eventTypeId, handler.Id);
			}
		}
		void PublishResult()
		{
			ReactorResult value2 = new ReactorResult
			{
				Partition = events.Partition,
				DeliveryId = events.DeliveryId,
				State = state,
				LastSuccessfulObservation = ConceptAs<ulong>.op_Implicit((ConceptAs<ulong>)lastSuccessfullyObservedEvent),
				ExceptionMessages = exceptionMessages.ToList(),
				ExceptionStackTrace = exceptionStackTrace
			};
			messages.Publish(new ReactorMessage(new OneOf<RegisterReactor, ReactorResult>(value2)));
		}
	}

	private async Task HandleReplayNotification(ReactorHandler handler, ReplayState replayState, string partition)
	{
		await using AsyncServiceScope serviceProviderScope = _serviceProvider.CreateAsyncScope();
		Catch<ActivatedArtifact> val = _artifactActivator.Activate(serviceProviderScope.ServiceProvider, handler.ReactorType);
		Exception ex = default(Exception);
		if (val.TryGetException(ref ex))
		{
			_logger.FailedActivatingReactorForReplayNotification(ex, handler.Id, replayState);
			return;
		}
		await using ActivatedArtifact activatedReactor = ((OneOfBase<ActivatedArtifact, Exception>)(object)val).AsT0;
		switch (replayState)
		{
		case ReplayState.BeginReplay:
			if (activatedReactor.Instance is ICanBeNotifiedWhenReplay canBeNotifiedWhenReplay2)
			{
				await canBeNotifiedWhenReplay2.BeginReplay();
			}
			break;
		case ReplayState.EndReplay:
			if (activatedReactor.Instance is ICanBeNotifiedWhenReplay canBeNotifiedWhenReplay)
			{
				await canBeNotifiedWhenReplay.EndReplay();
			}
			break;
		case ReplayState.BeginReplayPartition:
			if (activatedReactor.Instance is ICanBeNotifiedWhenPartitionReplayed canBeNotifiedWhenPartitionReplayed2)
			{
				await canBeNotifiedWhenPartitionReplayed2.BeginReplayPartition(partition);
			}
			break;
		case ReplayState.EndReplayPartition:
			if (activatedReactor.Instance is ICanBeNotifiedWhenPartitionReplayed canBeNotifiedWhenPartitionReplayed)
			{
				await canBeNotifiedWhenPartitionReplayed.EndReplayPartition(partition);
			}
			break;
		}
	}

	/// <inheritdoc />
	public Task<IReactorHandler> Register(Type reactorType)
	{
		RegisteredObserverMetadata.Validate(reactorType, ExplicitArtifactKind.Reactor, _clientArtifactsProvider);
		if (_eventStore.Reducers.GetAllHandlers().Any((IReducerHandler _) => _.ReducerType == reactorType))
		{
			throw new ConflictingArtifactKind(reactorType, "Reactor", "Reducer");
		}
		using (_registerLock.EnterScope())
		{
			ReactorRegistration reactorRegistration = CreateRegistrationFor(reactorType);
			AddRegistration(reactorRegistration);
			return Task.FromResult((IReactorHandler)reactorRegistration.Handler);
		}
	}

	/// <inheritdoc />
	public IReactorHandler GetHandlerFor(Type reactorType)
	{
		return _handlers.Values.FirstOrDefault((ReactorRegistration _) => _.Handler.ReactorType == reactorType)?.Handler ?? _handlers[ResolveId(reactorType)].Handler;
	}

	/// <inheritdoc />
	public Task<ReactorState> GetStateFor(Type reactorType)
	{
		return GetHandlerFor(reactorType).GetState();
	}

	/// <inheritdoc />
	public Task<JobId> Replay(Type reactorType)
	{
		return Replay(GetHandlerFor(reactorType).Id);
	}

	private ReactorId ResolveId(Type type)
	{
		return RegisteredObserverMetadata.ReactorIdFor(_clientArtifactsProvider, type);
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.1.0.9782' (yours is '9.1.0.7988')
