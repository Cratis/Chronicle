// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Dispatches delivered events to a reactor, independent of the transport that delivered them.
/// </summary>
/// <param name="eventStore"><see cref="IEventStore"/> the reactors belong to.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for resolving event types.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> to get instances of types.</param>
/// <param name="artifactActivator"><see cref="IClientArtifactsActivator"/> for creating artifact instances.</param>
/// <param name="middlewaresActivator"><see cref="IActivateReactorMiddlewares"/> to call.</param>
/// <param name="eventSerializer"><see cref="IEventSerializer"/> for deserializing events.</param>
/// <param name="activitySource"><see cref="IActivitySource{T}"/> for tracing reactor event handling.</param>
/// <param name="sideEffectHandlers"><see cref="IReactorSideEffectHandlers"/> for processing return values as side effects.</param>
/// <param name="reactorContextValuesBuilder"><see cref="IReactorContextValuesBuilder"/> for resolving append-metadata for side-effect events.</param>
/// <param name="argumentsResolver"><see cref="IReactorMethodArgumentsResolver"/> for resolving handler method arguments.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
internal sealed class ReactorDispatcher(
    IEventStore eventStore,
    IEventTypes eventTypes,
    IServiceProvider serviceProvider,
    IClientArtifactsActivator artifactActivator,
    IActivateReactorMiddlewares middlewaresActivator,
    IEventSerializer eventSerializer,
    IActivitySource<Reactors> activitySource,
    IReactorSideEffectHandlers sideEffectHandlers,
    IReactorContextValuesBuilder reactorContextValuesBuilder,
    IReactorMethodArgumentsResolver argumentsResolver,
    ILogger<Reactors> logger,
    ILoggerFactory loggerFactory)
{
#if CHRONICLE_ROLLING_DIAGNOSTICS
    /// <summary>
    /// Dispatches an isolated diagnostic candidate's batch with stream-local correlation.
    /// </summary>
    /// <param name="handler">The reactor handler.</param>
    /// <param name="handle">The fluent callback, if registered.</param>
    /// <param name="events">The delivered batch.</param>
    /// <param name="namespace">The event store namespace.</param>
    /// <param name="publish">The outbound result publisher.</param>
    /// <param name="cancellationToken">The shutdown token.</param>
    /// <param name="diagnosticStream">The diagnostic stream incarnation.</param>
    /// <param name="diagnosticBatch">The diagnostic batch ordinal.</param>
    /// <returns>The dispatch task.</returns>
    public async Task Dispatch(
        ReactorHandler handler,
        Func<ReactorEvent, CancellationToken, Task>? handle,
        EventsToObserve events,
        EventStoreNamespaceName @namespace,
        Action<ReactorResult> publish,
        CancellationToken cancellationToken,
        Guid diagnosticStream = default,
        long diagnosticBatch = 0)
#else
    /// <summary>
    /// Dispatches a batch of events, or a replay notification, to a reactor.
    /// </summary>
    /// <param name="handler">The <see cref="ReactorHandler"/> to dispatch to.</param>
    /// <param name="handle">The delegate to call for each event when the reactor is a delegate reactor; otherwise null.</param>
    /// <param name="events">The <see cref="EventsToObserve"/> to dispatch.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the events are delivered for.</param>
    /// <param name="publish">Callback for publishing the <see cref="ReactorResult"/> of the batch.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> signalling shutdown.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Dispatch(
        ReactorHandler handler,
        Func<ReactorEvent, CancellationToken, Task>? handle,
        EventsToObserve events,
        EventStoreNamespaceName @namespace,
        Action<ReactorResult> publish,
        CancellationToken cancellationToken)
#endif
    {
#if CHRONICLE_ROLLING_DIAGNOSTICS
        using var diagnosticScope = ReactorsDeliveryDiagnosticsLogging.BeginScope(logger, diagnosticStream, diagnosticBatch);
        ReactorsDeliveryDiagnosticsLogging.Members(logger, diagnosticStream, diagnosticBatch, events);
#endif
        if (events.ReplayState != ReplayState.None)
        {
            if (handle is null)
            {
                await HandleReplayNotification(handler, events.ReplayState, events.Partition);
            }
            return;
        }

        using var span = activitySource.Handle(
            eventStore.Name,
            @namespace,
            handler.EventSequenceId,
            handler.Id);

        var lastSuccessfullyObservedEvent = EventSequenceNumber.Unavailable;
        var exceptionMessages = Enumerable.Empty<string>();
        var exceptionStackTrace = string.Empty;
        var state = ObservationState.Success;

        if (handle is not null)
        {
            foreach (var @event in events.Events)
            {
                if (cancellationToken.IsCancellationRequested)
                {
#if CHRONICLE_ROLLING_DIAGNOSTICS
                    ReactorsDeliveryDiagnosticsLogging.Record(logger, 10, diagnosticStream, diagnosticBatch, events.DeliveryId, @event.Context.SequenceNumber);
#endif
                    return;
                }

                logger.EventReceived(@event.Context.EventType.Id, handler.Id);
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
                            await handle(delivered, cancellationToken);
                        }
                        finally
                        {
                            handler.EndHandling();
                        }
                    }
#if CHRONICLE_ROLLING_DIAGNOSTICS
                    var previousSequence = lastSuccessfullyObservedEvent.Value;
#endif
                    lastSuccessfullyObservedEvent = @event.Context.SequenceNumber;
#if CHRONICLE_ROLLING_DIAGNOSTICS
                    ReactorsDeliveryDiagnosticsLogging.Record(logger, 7, diagnosticStream, diagnosticBatch, events.DeliveryId, lastSuccessfullyObservedEvent.Value, previousSequence);
#endif
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
#if CHRONICLE_ROLLING_DIAGNOSTICS
                    ReactorsDeliveryDiagnosticsLogging.Record(logger, 10, diagnosticStream, diagnosticBatch, events.DeliveryId, @event.Context.SequenceNumber);
#endif
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

        await using var serviceProviderScope = serviceProvider.CreateAsyncScope();
        var activatedReactorResult = artifactActivator.Activate(serviceProviderScope.ServiceProvider, handler.ReactorType);
        if (activatedReactorResult.TryGetException(out var exception))
        {
            FailedActivatingReactor(exception);
            PublishResult();
            return;
        }

        await using var activatedReactor = activatedReactorResult.AsT0;
        await using var middlewares = middlewaresActivator.Activate(serviceProviderScope.ServiceProvider);
        var reactorInvoker = new ReactorInvoker(
            eventTypes,
            middlewares,
            handler.ReactorType,
            activatedReactor,
            loggerFactory.CreateLogger<ReactorInvoker>(),
            sideEffectHandlers,
            eventStore,
            reactorContextValuesBuilder,
            argumentsResolver,
            serviceProviderScope.ServiceProvider)
        {
            StoppingToken = cancellationToken,
            RegisteredId = handler.Id,
            RegisteredEventSequenceId = handler.EventSequenceId
        };

        foreach (var @event in events.Events)
        {
            logger.EventReceived(@event.Context.EventType.Id, handler.Id);

            try
            {
                var context = @event.Context.ToClient();

                var handlerEventType = handler.EventTypes.FirstOrDefault(et => et.Id == context.EventType.Id);
                var targetGeneration = handlerEventType?.Generation ?? context.EventType.Generation;
                var eventType = eventTypes.GetClrTypeFor(context.EventType.Id, targetGeneration);

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

                var content = await eventSerializer.Deserialize(eventType, JsonNode.Parse(contentJson)!.AsObject());

                var handleResult = await handler.OnNext(context, content, reactorInvoker);
                if (handleResult.IsFailed)
                {
                    FailedToHandleEvent(handleResult, @event.Context.EventType.Id);
                    break;
                }
#if CHRONICLE_ROLLING_DIAGNOSTICS
                var previousSequence = lastSuccessfullyObservedEvent.Value;
#endif
                lastSuccessfullyObservedEvent = @event.Context.SequenceNumber;
#if CHRONICLE_ROLLING_DIAGNOSTICS
                ReactorsDeliveryDiagnosticsLogging.Record(logger, 7, diagnosticStream, diagnosticBatch, events.DeliveryId, lastSuccessfullyObservedEvent.Value, previousSequence);
#endif
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
            span?.Activity?.RecordFailure("handling");
            var failureDetails = invocationResult.GetFailureDetails();
            exceptionMessages = failureDetails.Messages.ToArray();
            exceptionStackTrace = failureDetails.StackTrace;
            state = ObservationState.Failed;

            if (invocationResult.ExceptionResult.TryGetException(out var ex))
            {
                LogErrorWhileHandlingEvent(ex, eventTypeId);
            }
            else
            {
                var targetEventSourceIds = invocationResult.SideEffectFailure is not null
                    ? string.Join(", ", invocationResult.SideEffectFailure.GetTargetEventSourceIds())
                    : string.Empty;
                logger.ReactorSideEffectAppendFailed(eventTypeId, handler.Id, targetEventSourceIds, string.Join(Environment.NewLine, exceptionMessages));
            }
        }

        void LogErrorWhileHandlingEvent(Exception ex, EventTypeId eventTypeId)
        {
            span?.Activity?.RecordFailure(ex);
            if (ex.IsInterruptedByShutdown(cancellationToken))
            {
                logger.HandlingEventInterruptedByShutdown(ex, eventTypeId, handler.Id);
            }
            else
            {
                logger.ErrorWhileHandlingEvent(ex, eventTypeId, handler.Id);
            }
        }

        void FailedToHandleEventWithException(Exception ex, EventTypeId eventTypeId)
        {
#if CHRONICLE_ROLLING_DIAGNOSTICS
            ReactorsDeliveryDiagnosticsLogging.Record(logger, 11, diagnosticStream, diagnosticBatch, events.DeliveryId);
#endif
            LogErrorWhileHandlingEvent(ex, eventTypeId);
            exceptionMessages = ex.GetAllMessages();
            exceptionStackTrace = ex.StackTrace ?? string.Empty;
            state = ObservationState.Failed;
        }

        void FailedActivatingReactor(Exception ex)
        {
            span?.Activity?.RecordFailure(ex);
            exceptionMessages = ex.GetAllMessages();
            exceptionStackTrace = ex.StackTrace ?? string.Empty;
            state = ObservationState.Failed;
        }

        void PublishResult()
        {
            var result = new ReactorResult
            {
                Partition = events.Partition,
                DeliveryId = events.DeliveryId,
                State = state,
                LastSuccessfulObservation = lastSuccessfullyObservedEvent,
                ExceptionMessages = exceptionMessages.ToList(),
                ExceptionStackTrace = exceptionStackTrace
            };

#if CHRONICLE_ROLLING_DIAGNOSTICS
            ReactorsDeliveryDiagnosticsLogging.Record(logger, 8, diagnosticStream, diagnosticBatch, events.DeliveryId, lastSuccessfullyObservedEvent.Value, state: (int)state);
#endif
#if CHRONICLE_ROLLING_DIAGNOSTICS
            try
            {
                publish(result);
            }
            catch
            {
                ReactorsDeliveryDiagnosticsLogging.Record(logger, 11, diagnosticStream, diagnosticBatch, events.DeliveryId, lastSuccessfullyObservedEvent.Value, state: (int)state);
                throw;
            }
            ReactorsDeliveryDiagnosticsLogging.Record(logger, 9, diagnosticStream, diagnosticBatch, events.DeliveryId, lastSuccessfullyObservedEvent.Value, state: (int)state);
#else
            publish(result);
#endif
        }
    }

    async Task HandleReplayNotification(ReactorHandler handler, ReplayState replayState, string partition)
    {
        await using var serviceProviderScope = serviceProvider.CreateAsyncScope();
        var activatedReactorResult = artifactActivator.Activate(serviceProviderScope.ServiceProvider, handler.ReactorType);
        if (activatedReactorResult.TryGetException(out var ex))
        {
            logger.FailedActivatingReactorForReplayNotification(ex, handler.Id, replayState);
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
}
