using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Concepts;
using Cratis.Monads;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneOf;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Dispatches delivered events to a reactor, independent of the transport that delivered them.
/// </summary>
/// <param name="eventStore"><see cref="T:Cratis.Chronicle.IEventStore" /> the reactors belong to.</param>
/// <param name="eventTypes"><see cref="T:Cratis.Chronicle.Events.IEventTypes" /> for resolving event types.</param>
/// <param name="serviceProvider"><see cref="T:System.IServiceProvider" /> to get instances of types.</param>
/// <param name="artifactActivator"><see cref="T:Cratis.Chronicle.IClientArtifactsActivator" /> for creating artifact instances.</param>
/// <param name="middlewaresActivator"><see cref="T:Cratis.Chronicle.Reactors.IActivateReactorMiddlewares" /> to call.</param>
/// <param name="eventSerializer"><see cref="T:Cratis.Chronicle.Events.IEventSerializer" /> for deserializing events.</param>
/// <param name="activitySource"><see cref="T:Cratis.Traces.IActivitySource`1" /> for tracing reactor event handling.</param>
/// <param name="sideEffectHandlers"><see cref="T:Cratis.Chronicle.Reactors.SideEffects.IReactorSideEffectHandlers" /> for processing return values as side effects.</param>
/// <param name="reactorContextValuesBuilder"><see cref="T:Cratis.Chronicle.Reactors.SideEffects.IReactorContextValuesBuilder" /> for resolving append-metadata for side-effect events.</param>
/// <param name="argumentsResolver"><see cref="T:Cratis.Chronicle.Reactors.IReactorMethodArgumentsResolver" /> for resolving handler method arguments.</param>
/// <param name="logger"><see cref="T:Microsoft.Extensions.Logging.ILogger" /> for logging.</param>
/// <param name="loggerFactory"><see cref="T:Microsoft.Extensions.Logging.ILoggerFactory" /> for creating loggers.</param>
internal sealed class ReactorDispatcher(IEventStore eventStore, IEventTypes eventTypes, IServiceProvider serviceProvider, IClientArtifactsActivator artifactActivator, IActivateReactorMiddlewares middlewaresActivator, IEventSerializer eventSerializer, IActivitySource<Reactors> activitySource, IReactorSideEffectHandlers sideEffectHandlers, IReactorContextValuesBuilder reactorContextValuesBuilder, IReactorMethodArgumentsResolver argumentsResolver, ILogger<Reactors> logger, ILoggerFactory loggerFactory)
{
	/// <summary>
	/// Dispatches a batch of events, or a replay notification, to a reactor.
	/// </summary>
	/// <param name="handler">The <see cref="T:Cratis.Chronicle.Reactors.ReactorHandler" /> to dispatch to.</param>
	/// <param name="handle">The delegate to call for each event when the reactor is a delegate reactor; otherwise null.</param>
	/// <param name="events">The <see cref="T:Cratis.Chronicle.Contracts.Observation.EventsToObserve" /> to dispatch.</param>
	/// <param name="namespace">The <see cref="T:Cratis.Chronicle.EventStoreNamespaceName" /> the events are delivered for.</param>
	/// <param name="publish">Callback for publishing the <see cref="T:Cratis.Chronicle.Contracts.Observation.Reactors.ReactorResult" /> of the batch.</param>
	/// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> signalling shutdown.</param>
	/// <returns>Awaitable task.</returns>
	public async Task Dispatch(ReactorHandler handler, Func<ReactorEvent, CancellationToken, Task>? handle, EventsToObserve events, EventStoreNamespaceName @namespace, Action<ReactorResult> publish, CancellationToken cancellationToken)
	{
		if (events.ReplayState != ReplayState.None)
		{
			if (handle == null)
			{
				await HandleReplayNotification(handler, events.ReplayState, events.Partition);
			}
			return;
		}
		IActivityScope<Reactors> span = activitySource.Handle(eventStore.Name, ConceptAs<string>.op_Implicit((ConceptAs<string>)(object)@namespace), ConceptAs<string>.op_Implicit((ConceptAs<string>)(object)handler.EventSequenceId), ConceptAs<string>.op_Implicit((ConceptAs<string>)(object)handler.Id));
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
			if (handle != null)
			{
				foreach (Cratis.Chronicle.Contracts.Events.AppendedEvent @event in events.Events)
				{
					if (cancellationToken.IsCancellationRequested)
					{
						return;
					}
					logger.EventReceived(@event.Context.EventType.Id, handler.Id);
					try
					{
						ReactorEvent reactorEvent = new ReactorEvent(@event.Context.ToClient(), JsonNode.Parse(@event.Content).AsObject(), new Dictionary<int, string>(@event.GenerationalContent));
						using (handler.BeginHandlingScope(reactorEvent.Context))
						{
							_ = 1;
							try
							{
								await handle(reactorEvent, cancellationToken);
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
			await using AsyncServiceScope serviceProviderScope = serviceProvider.CreateAsyncScope();
			Catch<ActivatedArtifact> val = artifactActivator.Activate(serviceProviderScope.ServiceProvider, handler.ReactorType);
			Exception ex3 = default(Exception);
			if (val.TryGetException(ref ex3))
			{
				FailedActivatingReactor(ex3);
				PublishResult();
				return;
			}
			await using ActivatedArtifact activatedReactor = ((OneOfBase<ActivatedArtifact, Exception>)(object)val).AsT0;
			await using IReactorMiddlewares middlewares = middlewaresActivator.Activate(serviceProviderScope.ServiceProvider);
			ReactorInvoker reactorInvoker = new ReactorInvoker(eventTypes, middlewares, handler.ReactorType, activatedReactor, loggerFactory.CreateLogger<ReactorInvoker>(), sideEffectHandlers, eventStore, reactorContextValuesBuilder, argumentsResolver, serviceProviderScope.ServiceProvider)
			{
				StoppingToken = cancellationToken,
				RegisteredId = handler.Id,
				RegisteredEventSequenceId = handler.EventSequenceId
			};
			foreach (Cratis.Chronicle.Contracts.Events.AppendedEvent @event in events.Events)
			{
				logger.EventReceived(@event.Context.EventType.Id, handler.Id);
				try
				{
					Cratis.Chronicle.Events.EventContext context = @event.Context.ToClient();
					EventTypeGeneration eventTypeGeneration = handler.EventTypes.FirstOrDefault((Cratis.Chronicle.Events.EventType et) => et.Id == context.EventType.Id)?.Generation ?? context.EventType.Generation;
					Type clrTypeFor = eventTypes.GetClrTypeFor(context.EventType.Id, eventTypeGeneration);
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
					object content = await eventSerializer.Deserialize(clrTypeFor, JsonNode.Parse(json).AsObject());
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
			if (span != null)
			{
				((IDisposable)span).Dispose();
			}
		}
		void FailedActivatingReactor(Exception ex5)
		{
			span?.Activity?.RecordFailure(ex5);
			exceptionMessages = ExceptionExtensions.GetAllMessages(ex5);
			exceptionStackTrace = ex5.StackTrace ?? string.Empty;
			state = ObservationState.Failed;
		}
		void FailedToHandleEvent(ReactorInvocationResult invocationResult, EventTypeId eventTypeId)
		{
			span?.Activity?.RecordFailure("handling");
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
				logger.ReactorSideEffectAppendFailed(eventTypeId, handler.Id, targetEventSourceIds, string.Join(Environment.NewLine, exceptionMessages));
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
			span?.Activity?.RecordFailure(ex5);
			if (ex5.IsInterruptedByShutdown(cancellationToken))
			{
				logger.HandlingEventInterruptedByShutdown(ex5, eventTypeId, handler.Id);
			}
			else
			{
				logger.ErrorWhileHandlingEvent(ex5, eventTypeId, handler.Id);
			}
		}
		void PublishResult()
		{
			ReactorResult obj = new ReactorResult
			{
				Partition = events.Partition,
				DeliveryId = events.DeliveryId,
				State = state,
				LastSuccessfulObservation = ConceptAs<ulong>.op_Implicit((ConceptAs<ulong>)lastSuccessfullyObservedEvent),
				ExceptionMessages = exceptionMessages.ToList(),
				ExceptionStackTrace = exceptionStackTrace
			};
			publish(obj);
		}
	}

	private async Task HandleReplayNotification(ReactorHandler handler, ReplayState replayState, string partition)
	{
		await using AsyncServiceScope serviceProviderScope = serviceProvider.CreateAsyncScope();
		Catch<ActivatedArtifact> val = artifactActivator.Activate(serviceProviderScope.ServiceProvider, handler.ReactorType);
		Exception ex = default(Exception);
		if (val.TryGetException(ref ex))
		{
			logger.FailedActivatingReactorForReplayNotification(ex, handler.Id, replayState);
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
}
You are not using the latest version of the tool, please update.
Latest version is '11.2.0.10050' (yours is '9.1.0.7988')
