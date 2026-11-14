using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Chronicle.Registrations;
using Cratis.Monads;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents an implementation of <see cref="T:Cratis.Chronicle.Reactors.IReactorInvoker" />.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="T:Cratis.Chronicle.Reactors.ReactorInvoker" /> class.
/// </remarks>
/// <param name="eventTypes"><see cref="T:Cratis.Chronicle.Events.IEventTypes" /> for mapping types.</param>
/// <param name="middlewares"><see cref="T:Cratis.Chronicle.Reactors.IReactorMiddlewares" /> to call.</param>
/// <param name="targetType">Type of Reactor.</param>
/// <param name="activatedReactor">The <see cref="T:Cratis.Chronicle.ActivatedArtifact" /> activated reactor.</param>
/// <param name="logger"><see cref="T:Microsoft.Extensions.Logging.ILogger" /> for logging.</param>
/// <param name="sideEffectHandlers">
/// Optional <see cref="T:Cratis.Chronicle.Reactors.SideEffects.IReactorSideEffectHandlers" /> used to process events returned by handler methods.
/// When <see langword="null" />, a non-null return value fails the invocation.
/// </param>
/// <param name="eventStore">
/// Optional <see cref="T:Cratis.Chronicle.IEventStore" /> supplied to side effect handlers when appending events.
/// When <see langword="null" />, a non-null return value fails the invocation even if handlers are registered.
/// </param>
/// <param name="reactorContextValuesBuilder">
/// Optional <see cref="T:Cratis.Chronicle.Reactors.SideEffects.IReactorContextValuesBuilder" /> used to resolve append-metadata for side-effect events.
/// When <see langword="null" />, no values are resolved and append-metadata falls back to the triggering event.
/// </param>
/// <param name="argumentsResolver">
/// Optional <see cref="T:Cratis.Chronicle.Reactors.IReactorMethodArgumentsResolver" /> used to resolve handler method arguments. When
/// <see langword="null" />, a default <see cref="T:Cratis.Chronicle.Reactors.ReactorMethodArgumentsResolver" /> is used.
/// </param>
/// <param name="serviceProvider">
/// Optional <see cref="T:System.IServiceProvider" /> used to resolve handler method dependencies that are neither the
/// <see cref="T:Cratis.Chronicle.Events.EventContext" /> nor a read model.
/// </param>
public class ReactorInvoker : IReactorInvoker
{
	/// <summary>
	/// The handler methods of a reactor, split by whether they take over during a replay.
	/// </summary>
	/// <param name="Live">The handlers to run as events happen.</param>
	/// <param name="Replay">The handlers marked with <see cref="T:Cratis.Chronicle.Reactors.ReplayAttribute" />, which take over during a replay.</param>
	/// <param name="OnceOnly">The handlers marked with <see cref="T:Cratis.Chronicle.Reactors.OnceOnlyAttribute" />, which never run for a replayed event.</param>
	private record HandlerMethods(FrozenDictionary<Type, MethodInfo> Live, FrozenDictionary<Type, MethodInfo> Replay, FrozenSet<MethodInfo> OnceOnly)
	{
		/// <summary>
		/// Gets every event type handled, whichever path handles it. An event type handled only by a replay
		/// handler still has to be subscribed to, or the replay would have nothing to deliver.
		/// </summary>
		public IEnumerable<Type> AllEventTypes => Live.Keys.Union(Replay.Keys);

		/// <summary>
		/// Resolves the handler to run for an event type, given the state the event is being observed in.
		/// </summary>
		/// <param name="eventType">The event <see cref="T:System.Type" /> being observed.</param>
		/// <param name="observationState">The <see cref="T:Cratis.Chronicle.Events.EventObservationState" /> the event arrives in.</param>
		/// <returns>
		/// The handler to run, or the handler that was deliberately skipped because it is marked
		/// <see cref="T:Cratis.Chronicle.Reactors.OnceOnlyAttribute" /> and the event is being replayed. Both are absent when the reactor
		/// has no handler for the event type at all.
		/// </returns>
		/// <remarks>
		/// <see cref="T:Cratis.Chronicle.Reactors.OnceOnlyAttribute" /> on a handler method means that handler is excluded from replay - the
		/// class-level placement keeps the whole reactor from being replayed, and the method-level placement can
		/// only be honored here, where the event arrives carrying the state it is observed in.
		/// </remarks>
		public (MethodInfo? Method, MethodInfo? SkippedForReplay) Resolve(Type eventType, EventObservationState observationState)
		{
			bool flag = observationState.HasFlag(EventObservationState.Replay);
			if (flag && Replay.TryGetValue(eventType, out MethodInfo value))
			{
				if (!OnceOnly.Contains(value))
				{
					return (Method: value, SkippedForReplay: null);
				}
				return (Method: null, SkippedForReplay: value);
			}
			if (!Live.TryGetValue(eventType, out MethodInfo value2))
			{
				return (Method: null, SkippedForReplay: null);
			}
			if (!flag || !OnceOnly.Contains(value2))
			{
				return (Method: value2, SkippedForReplay: null);
			}
			return (Method: null, SkippedForReplay: value2);
		}
	}

	private static class MethodsByEventType
	{
		/// <summary>
		/// Builds dispatch for the supplied registry snapshot and side-effect handlers.
		/// </summary>
		/// <param name="targetType">The reactor type.</param>
		/// <param name="eventTypes">The current registry's CLR types.</param>
		/// <param name="sideEffectHandlers">The current side-effect handlers.</param>
		/// <returns>The handler map.</returns>
		/// <exception cref="T:Cratis.Chronicle.Reactors.InvalidReactorHandlerReturnType">A public handler has an unsupported return type.</exception>
		public static HandlerMethods Get(Type targetType, IEnumerable<Type> eventTypes, IReactorSideEffectHandlers? sideEffectHandlers = null)
		{
			Dictionary<Type, MethodInfo> dictionary = new Dictionary<Type, MethodInfo>();
			Dictionary<Type, MethodInfo> dictionary2 = new Dictionary<Type, MethodInfo>();
			HashSet<MethodInfo> hashSet = new HashSet<MethodInfo>();
			foreach (MethodInfo item in targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ByPrecedence())
			{
				if (!item.IsEventHandlerMethod(eventTypes, sideEffectHandlers))
				{
					if (item.IsPublic && item.HasEventHandlerShape(eventTypes))
					{
						throw new InvalidReactorHandlerReturnType(targetType, item.Name, item.ReturnType);
					}
					continue;
				}
				Dictionary<Type, MethodInfo> dictionary3 = (item.IsDefined(typeof(ReplayAttribute), inherit: false) ? dictionary2 : dictionary);
				if (item.IsDefined(typeof(OnceOnlyAttribute), inherit: false))
				{
					hashSet.Add(item);
				}
				foreach (Type eventType in item.GetParameters()[0].ParameterType.GetEventTypes(eventTypes))
				{
					dictionary3.TryAdd(eventType, item);
				}
			}
			return new HandlerMethods(dictionary.ToFrozenDictionary(), dictionary2.ToFrozenDictionary(), hashSet.ToFrozenSet());
		}
	}

	private static readonly EventTypeRegistryCache<HandlerMethods> _methodsByEventTypeCache = new EventTypeRegistryCache<HandlerMethods>();

	private readonly HandlerMethods _methodsByEventType;

	private readonly IReactorMethodArgumentsResolver _argumentsResolver;

	/// <summary>
	/// Gets the token that is cancelled when the reactor stops observing, typically because the client or host is shutting down.
	/// </summary>
	/// <remarks>
	/// A handler whose call to the kernel is cancelled or disposed after this is cancelled was interrupted by the shutdown and
	/// did not fail, so it is not logged as an error. The invocation still reports a failure, since the event was not handled.
	/// </remarks>
	internal CancellationToken StoppingToken { get; init; }

	/// <summary>
	/// Gets the owning registration's resolved observer identity, including explicit aliases.
	/// </summary>
	internal ReactorId? RegisteredId { get; init; }

	/// <summary>
	/// Gets the owning registration's resolved event sequence.
	/// </summary>
	internal EventSequenceId? RegisteredEventSequenceId { get; init; }

	/// <summary>
	/// Represents an implementation of <see cref="T:Cratis.Chronicle.Reactors.IReactorInvoker" />.
	/// </summary>
	/// <remarks>
	/// Initializes a new instance of the <see cref="T:Cratis.Chronicle.Reactors.ReactorInvoker" /> class.
	/// </remarks>
	/// <param name="eventTypes"><see cref="T:Cratis.Chronicle.Events.IEventTypes" /> for mapping types.</param>
	/// <param name="middlewares"><see cref="T:Cratis.Chronicle.Reactors.IReactorMiddlewares" /> to call.</param>
	/// <param name="targetType">Type of Reactor.</param>
	/// <param name="activatedReactor">The <see cref="T:Cratis.Chronicle.ActivatedArtifact" /> activated reactor.</param>
	/// <param name="logger"><see cref="T:Microsoft.Extensions.Logging.ILogger" /> for logging.</param>
	/// <param name="sideEffectHandlers">
	/// Optional <see cref="T:Cratis.Chronicle.Reactors.SideEffects.IReactorSideEffectHandlers" /> used to process events returned by handler methods.
	/// When <see langword="null" />, a non-null return value fails the invocation.
	/// </param>
	/// <param name="eventStore">
	/// Optional <see cref="T:Cratis.Chronicle.IEventStore" /> supplied to side effect handlers when appending events.
	/// When <see langword="null" />, a non-null return value fails the invocation even if handlers are registered.
	/// </param>
	/// <param name="reactorContextValuesBuilder">
	/// Optional <see cref="T:Cratis.Chronicle.Reactors.SideEffects.IReactorContextValuesBuilder" /> used to resolve append-metadata for side-effect events.
	/// When <see langword="null" />, no values are resolved and append-metadata falls back to the triggering event.
	/// </param>
	/// <param name="argumentsResolver">
	/// Optional <see cref="T:Cratis.Chronicle.Reactors.IReactorMethodArgumentsResolver" /> used to resolve handler method arguments. When
	/// <see langword="null" />, a default <see cref="T:Cratis.Chronicle.Reactors.ReactorMethodArgumentsResolver" /> is used.
	/// </param>
	/// <param name="serviceProvider">
	/// Optional <see cref="T:System.IServiceProvider" /> used to resolve handler method dependencies that are neither the
	/// <see cref="T:Cratis.Chronicle.Events.EventContext" /> nor a read model.
	/// </param>
	public ReactorInvoker(IEventTypes eventTypes, IReactorMiddlewares middlewares, Type targetType, ActivatedArtifact activatedReactor, ILogger<ReactorInvoker> logger, IReactorSideEffectHandlers? sideEffectHandlers = null, IEventStore? eventStore = null, IReactorContextValuesBuilder? reactorContextValuesBuilder = null, IReactorMethodArgumentsResolver? argumentsResolver = null, IServiceProvider? serviceProvider = null)
	{
		<eventTypes>P = eventTypes;
		<middlewares>P = middlewares;
		<targetType>P = targetType;
		<activatedReactor>P = activatedReactor;
		<logger>P = logger;
		<sideEffectHandlers>P = sideEffectHandlers;
		<eventStore>P = eventStore;
		<reactorContextValuesBuilder>P = reactorContextValuesBuilder;
		<serviceProvider>P = serviceProvider;
		_methodsByEventType = _methodsByEventTypeCache.Get(<eventTypes>P, <targetType>P, <sideEffectHandlers>P, (IEnumerable<Type> types) => MethodsByEventType.Get(<targetType>P, types, <sideEffectHandlers>P));
		_argumentsResolver = argumentsResolver ?? new ReactorMethodArgumentsResolver();
		base..ctor();
	}

	/// <summary>
	/// Gets all <see cref="T:Cratis.Chronicle.Events.EventType" /> for a specific reactor type.
	/// </summary>
	/// <param name="eventTypes"><see cref="T:Cratis.Chronicle.Events.IEventTypes" /> for looking up event types.</param>
	/// <param name="reactorType">The reactor <see cref="T:System.Type" /> to get event types for.</param>
	/// <returns>Collection of discovered <see cref="T:Cratis.Chronicle.Events.EventType" />.</returns>
	public static IImmutableList<EventType> GetEventTypesFor(IEventTypes eventTypes, Type reactorType)
	{
		return GetEventTypesFor(eventTypes, reactorType, null);
	}

	/// <summary>
	/// Gets the event types for a reactor, including methods returning handler-claimed synchronous side effects.
	/// </summary>
	/// <param name="eventTypes">Registry of known event types.</param>
	/// <param name="reactorType">The reactor type to inspect.</param>
	/// <param name="sideEffectHandlers">Handlers claiming synchronous return types.</param>
	/// <returns>The event types handled by the reactor.</returns>
	public static IImmutableList<EventType> GetEventTypesFor(IEventTypes eventTypes, Type reactorType, IReactorSideEffectHandlers? sideEffectHandlers)
	{
		return _methodsByEventTypeCache.Get(eventTypes, reactorType, sideEffectHandlers, (IEnumerable<Type> types) => MethodsByEventType.Get(reactorType, types, sideEffectHandlers)).AllEventTypes.Select(eventTypes.GetEventTypeFor).ToImmutableList();
	}

	/// <inheritdoc />
	public async Task<ReactorInvocationResult> Invoke(object content, EventContext eventContext)
	{
		ReactorId reactorId = RegisteredId ?? <targetType>P.GetReactorId();
		string eventTypeName = content.GetType().Name;
		ReactorInvocationResult result;
		try
		{
			_ = 3;
			try
			{
				Type type = content.GetType();
				var (method, methodInfo) = _methodsByEventType.Resolve(type, eventContext.ObservationState);
				if ((object)methodInfo != null)
				{
					<logger>P.ReactorHandlerSkippedForReplay(reactorId, methodInfo.Name, eventTypeName);
					goto IL_0384;
				}
				if ((object)method == null)
				{
					<logger>P.ReactorNoHandlerFound(reactorId, eventTypeName);
					goto IL_0384;
				}
				await <middlewares>P.BeforeInvoke(reactorId, eventContext, content);
				object[] array = ((!(_argumentsResolver is IRegisteredReactorMethodArgumentsResolver registeredReactorMethodArgumentsResolver) || !method.GetParameters().Any((ParameterInfo parameter) => parameter.ParameterType == typeof(ReactorDelivery))) ? (await _argumentsResolver.Resolve(method, <activatedReactor>P.Instance, content, eventContext, <eventStore>P, <serviceProvider>P)) : (await registeredReactorMethodArgumentsResolver.Resolve(method, <activatedReactor>P.Instance, content, eventContext, <eventStore>P, <serviceProvider>P, ResolveDelivery(reactorId, eventContext))));
				object[] parameters = array;
				object returnValue = method.Invoke(<activatedReactor>P.Instance, parameters);
				ReactorInvocationResult reactorInvocationResult = await HandleReturnValue(method, returnValue, eventContext, reactorId);
				if ((object)reactorInvocationResult == null)
				{
					goto IL_0384;
				}
				result = reactorInvocationResult;
				goto end_IL_0074;
				IL_0384:
				result = ReactorInvocationResult.Success();
				end_IL_0074:;
			}
			catch (Exception exception)
			{
				if (exception.IsInterruptedByShutdown(StoppingToken))
				{
					<logger>P.ReactorInterruptedByShutdown(reactorId, eventTypeName, exception);
				}
				else
				{
					<logger>P.ReactorFailed(reactorId, eventTypeName, exception);
				}
				result = ReactorInvocationResult.FromException(exception);
			}
		}
		finally
		{
			try
			{
				await <middlewares>P.AfterInvoke(reactorId, eventContext, content);
			}
			catch (Exception exception2)
			{
				<logger>P.ReactorMiddlewareAfterInvokeFailed(reactorId, eventTypeName, exception2);
			}
		}
		return result;
	}

	private ReactorDelivery ResolveDelivery(ReactorId reactorId, EventContext eventContext)
	{
		EventSequenceId eventSequence = RegisteredEventSequenceId ?? RegisteredObserverMetadata.SequenceFor(<targetType>P, <targetType>P.GetCustomAttribute<ReactorAttribute>()?.EventSequenceId, <eventTypes>P, <eventStore>P?.Name ?? eventContext.EventStore, () => _methodsByEventType.AllEventTypes.Select(<eventTypes>P.GetEventTypeFor));
		return ReactorDelivery.For(reactorId, eventSequence, eventContext)with
		{
			EffectEpoch = (<targetType>P.GetCustomAttribute<ReactorAttribute>()?.EffectEpoch ?? 0)
		};
	}

	private async Task<ReactorInvocationResult?> HandleReturnValue(MethodInfo method, object? returnValue, EventContext eventContext, ReactorId reactorId)
	{
		if (method.ReturnType == typeof(void))
		{
			return null;
		}
		if (returnValue is Task task)
		{
			await task;
			if (!method.ReturnType.IsGenericType)
			{
				return null;
			}
			object obj = task.GetType().GetProperty("Result")?.GetValue(task);
			if (obj == null)
			{
				return null;
			}
			if (<sideEffectHandlers>P == null || <eventStore>P == null)
			{
				return ReactorInvocationResult.FromException(new UnhandledReactorReturnValue(<targetType>P, obj.GetType()));
			}
			ReactorContext reactorContext = new ReactorContext(eventContext, <activatedReactor>P.Instance, BuildValues(eventContext))
			{
				EventStore = <eventStore>P,
				Delivery = ResolveDelivery(reactorId, eventContext),
				Logger = <logger>P
			};
			if (<sideEffectHandlers>P.CanHandle(reactorContext, <eventStore>P, obj))
			{
				Result<ReactorSideEffectFailure> val = await <sideEffectHandlers>P.Handle(reactorContext, <eventStore>P, obj);
				ReactorSideEffectFailure reactorSideEffectFailure = default(ReactorSideEffectFailure);
				if (!val.IsSuccess && val.TryGetError(ref reactorSideEffectFailure) && (object)reactorSideEffectFailure != null)
				{
					return ReactorInvocationResult.FromSideEffectFailure(reactorSideEffectFailure);
				}
				return null;
			}
			return ReactorInvocationResult.FromException(new UnhandledReactorReturnValue(<targetType>P, obj.GetType()));
		}
		if (returnValue == null)
		{
			return null;
		}
		if (<sideEffectHandlers>P == null || <eventStore>P == null)
		{
			return ReactorInvocationResult.FromException(new UnhandledReactorReturnValue(<targetType>P, returnValue.GetType()));
		}
		ReactorContext reactorContext2 = new ReactorContext(eventContext, <activatedReactor>P.Instance, BuildValues(eventContext))
		{
			EventStore = <eventStore>P,
			Delivery = ResolveDelivery(reactorId, eventContext),
			Logger = <logger>P
		};
		if (<sideEffectHandlers>P.CanHandle(reactorContext2, <eventStore>P, returnValue))
		{
			Result<ReactorSideEffectFailure> val2 = await <sideEffectHandlers>P.Handle(reactorContext2, <eventStore>P, returnValue);
			ReactorSideEffectFailure reactorSideEffectFailure2 = default(ReactorSideEffectFailure);
			if (!val2.IsSuccess && val2.TryGetError(ref reactorSideEffectFailure2) && (object)reactorSideEffectFailure2 != null)
			{
				return ReactorInvocationResult.FromSideEffectFailure(reactorSideEffectFailure2);
			}
			return null;
		}
		return ReactorInvocationResult.FromException(new UnhandledReactorReturnValue(<targetType>P, returnValue.GetType()));
	}

	private ReactorContextValues BuildValues(EventContext eventContext)
	{
		return <reactorContextValuesBuilder>P?.Build(<activatedReactor>P.Instance, eventContext) ?? ReactorContextValues.Empty;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.2.0.10050' (yours is '9.1.0.7988')
