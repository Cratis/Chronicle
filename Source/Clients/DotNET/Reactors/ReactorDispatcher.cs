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
    /// <summary>
    /// Gets or sets the client span naming convention.
    /// </summary>
    internal ClientSpanNames SpanNames { get; set; }

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
                await HandleReplayNotification(handler, @namespace, events.ReplayState, events.Partition);
            }
            return;
        }

        using var span = activitySource.Handle(
            new(eventStore.Name, @namespace, handler.EventSequenceId, handler.Id),
            SpanNames);

        var partitions = events.PartitionsOf();
        var outcomes = new List<PartitionOutcome>(partitions.Count);

        if (handle is not null)
        {
            foreach (var partition in partitions)
            {
                var outcome = await DispatchToDelegate(partition);
                if (outcome is null)
                {
                    return;
                }

                outcomes.Add(outcome);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                PublishResult();
            }
            return;
        }

        ObserverDeliveryScope delivery;
        try
        {
            delivery = ObserverDeliveryScope.Enter(serviceProvider, DeliveryContextFor(handler, @namespace));
        }
        catch (Exception enterException)
        {
            span?.Activity?.RecordFailure(enterException);
            outcomes.AddRange(partitions.Select(_ => PartitionOutcome.Failed(_.Partition ?? string.Empty, enterException)));
            PublishResult();
            return;
        }

        using var deliveryScope = delivery;
        await using var serviceProviderScope = serviceProvider.CreateAsyncScope();
        var activatedReactorResult = artifactActivator.Activate(serviceProviderScope.ServiceProvider, handler.ReactorType);
        if (activatedReactorResult.TryGetException(out var exception))
        {
            span?.Activity?.RecordFailure(exception);
            outcomes.AddRange(partitions.Select(_ => PartitionOutcome.Failed(_.Partition ?? string.Empty, exception)));
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

        if (reactorInvoker.Batches.Count == 0)
        {
            foreach (var partition in partitions)
            {
                outcomes.Add(await DispatchToInstance(partition, reactorInvoker));
            }
        }
        else
        {
            outcomes.AddRange(await DispatchWithBatches(reactorInvoker));
        }

        PublishResult();

        async Task<IEnumerable<PartitionOutcome>> DispatchWithBatches(ReactorInvoker reactorInvoker)
        {
            var batchEventTypeIds = reactorInvoker.Batches
                .SelectMany(_ => _.EventTypes)
                .Select(_ => eventTypes.GetEventTypeFor(_).Id)
                .ToHashSet();
            var problems = new List<PartitionProblem>();
            var batchEvents = new List<(string Partition, EventAndContext Event)>();
            foreach (var partition in partitions)
            {
                foreach (var @event in partition.Events.Where(_ => batchEventTypeIds.Contains(_.Context.EventType.Id)))
                {
                    try
                    {
                        var (context, content) = await Deserialize(@event);
                        batchEvents.Add((partition.Partition ?? string.Empty, new EventAndContext(content, context)));
                    }
                    catch (Exception ex)
                    {
                        problems.Add(PartitionProblem.Failed(partition.Partition ?? string.Empty, @event.Context.SequenceNumber, ex));
                    }
                }
            }

            batchEvents.Sort((left, right) => left.Event.Context.SequenceNumber.Value.CompareTo(right.Event.Context.SequenceNumber.Value));
            problems.AddRange(await ReactorBatches.Run(reactorInvoker, batchEvents, CreateBatchContext(), events.Partitions.Count > 0));

            foreach (var partition in partitions)
            {
                var perEvent = await DispatchToInstance(partition, reactorInvoker, batchEventTypeIds);
                if (perEvent.State == ObservationState.Failed)
                {
                    problems.Add(new(perEvent.Partition, perEvent.FailedAt, ObservationState.Failed, perEvent.ExceptionMessages, perEvent.ExceptionStackTrace, TimeSpan.Zero));
                }
            }

            return partitions.Select(partition => PartitionSettlement.Settle(
                partition.Partition ?? string.Empty,
                [.. partition.Events.Select(_ => (EventSequenceNumber)_.Context.SequenceNumber)],
                problems));
        }

        BatchContext CreateBatchContext()
        {
            var allEvents = partitions.SelectMany(_ => _.Events).Select(_ => _.Context.SequenceNumber).ToArray();
            return new(
                handler.Id.Value,
                eventStore.Name,
                @namespace,
                handler.EventSequenceId,
                events.Origin.ToClient(),
                [.. partitions.Select(_ => new EventSourceId(_.Partition ?? string.Empty))],
                allEvents.Length,
                allEvents.Length > 0 ? allEvents.Min() : EventSequenceNumber.Unavailable,
                allEvents.Length > 0 ? allEvents.Max() : EventSequenceNumber.Unavailable,
                events.DeliverWithinMilliseconds > 0 ? DateTimeOffset.UtcNow.AddMilliseconds(events.DeliverWithinMilliseconds) : null,
                cancellationToken);
        }

        async Task<(EventContext Context, object Content)> Deserialize(Contracts.Events.AppendedEvent @event)
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

            return (context, await eventSerializer.Deserialize(eventType, JsonNode.Parse(contentJson)!.AsObject()));
        }

        async Task<PartitionOutcome?> DispatchToDelegate(PartitionEvents partition)
        {
            var lastSuccessfullyObservedEvent = EventSequenceNumber.Unavailable;
            foreach (var @event in partition.Events)
            {
                if (cancellationToken.IsCancellationRequested)
                {
#if CHRONICLE_ROLLING_DIAGNOSTICS
                    ReactorsDeliveryDiagnosticsLogging.Record(logger, 10, diagnosticStream, diagnosticBatch, events.DeliveryId, @event.Context.SequenceNumber);
#endif
                    return null;
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
                    return null;
                }
                catch (Exception ex)
                {
                    return FailedWithException(partition, lastSuccessfullyObservedEvent, ex, @event.Context.EventType.Id);
                }
            }

            return Succeeded(partition, lastSuccessfullyObservedEvent);
        }

        async Task<PartitionOutcome> DispatchToInstance(PartitionEvents partition, ReactorInvoker reactorInvoker, IReadOnlySet<EventTypeId>? handledByBatches = null)
        {
            var lastSuccessfullyObservedEvent = EventSequenceNumber.Unavailable;
            foreach (var @event in partition.Events.Where(_ => handledByBatches?.Contains(_.Context.EventType.Id) != true))
            {
                logger.EventReceived(@event.Context.EventType.Id, handler.Id);

                try
                {
                    var (context, content) = await Deserialize(@event);

                    var handleResult = await handler.OnNext(context, content, reactorInvoker);
                    if (handleResult.IsFailed)
                    {
                        return FailedToHandleEvent(partition, lastSuccessfullyObservedEvent, handleResult, @event.Context.EventType.Id) with { FailedAt = @event.Context.SequenceNumber };
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
                    return FailedWithException(partition, lastSuccessfullyObservedEvent, ex, @event.Context.EventType.Id) with { FailedAt = @event.Context.SequenceNumber };
                }
            }

            return Succeeded(partition, lastSuccessfullyObservedEvent);
        }

        static PartitionOutcome Succeeded(PartitionEvents partition, EventSequenceNumber lastSuccessfullyObservedEvent) =>
            new(partition.Partition ?? string.Empty, ObservationState.Success, lastSuccessfullyObservedEvent, [], string.Empty);

        PartitionOutcome FailedToHandleEvent(PartitionEvents partition, EventSequenceNumber lastSuccessfullyObservedEvent, ReactorInvocationResult invocationResult, EventTypeId eventTypeId)
        {
            span?.Activity?.RecordFailure("handling");
            var failureDetails = invocationResult.GetFailureDetails();
            var exceptionMessages = failureDetails.Messages.ToArray();

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

            return new(partition.Partition ?? string.Empty, ObservationState.Failed, lastSuccessfullyObservedEvent, exceptionMessages, failureDetails.StackTrace);
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

        PartitionOutcome FailedWithException(PartitionEvents partition, EventSequenceNumber lastSuccessfullyObservedEvent, Exception ex, EventTypeId eventTypeId)
        {
#if CHRONICLE_ROLLING_DIAGNOSTICS
            ReactorsDeliveryDiagnosticsLogging.Record(logger, 11, diagnosticStream, diagnosticBatch, events.DeliveryId);
#endif
            LogErrorWhileHandlingEvent(ex, eventTypeId);
            return new(partition.Partition ?? string.Empty, ObservationState.Failed, lastSuccessfullyObservedEvent, ex.GetAllMessages(), ex.StackTrace ?? string.Empty);
        }

        void PublishResult()
        {
            var result = events.Partitions.Count > 0
                ? new ReactorResult
                {
                    DeliveryId = events.DeliveryId,
                    Partitions = [.. outcomes.Select(_ => _.ToContract())]
                }
                : new ReactorResult
                {
                    Partition = events.Partition,
                    DeliveryId = events.DeliveryId,
                    State = outcomes[0].State == ObservationState.Deferred ? ObservationState.Failed : outcomes[0].State,
                    LastSuccessfulObservation = outcomes[0].LastSuccessfulObservation,
                    ExceptionMessages = [.. outcomes[0].ExceptionMessages],
                    ExceptionStackTrace = outcomes[0].ExceptionStackTrace
                };
#if CHRONICLE_ROLLING_DIAGNOSTICS
            var lastSuccessfullyObservedEvent = outcomes.Count > 0 ? outcomes[^1].LastSuccessfulObservation : EventSequenceNumber.Unavailable;
            var state = outcomes.Count > 0 ? outcomes[^1].State : ObservationState.Success;
            ReactorsDeliveryDiagnosticsLogging.Record(logger, 8, diagnosticStream, diagnosticBatch, events.DeliveryId, lastSuccessfullyObservedEvent.Value, state: (int)state);
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

    ObserverDeliveryContext DeliveryContextFor(ReactorHandler handler, EventStoreNamespaceName @namespace) =>
        new(eventStore.Name, @namespace, new(handler.Id.Value), ObserverDeliveryKind.Reactor, handler.ReactorType);

    async Task HandleReplayNotification(ReactorHandler handler, EventStoreNamespaceName @namespace, ReplayState replayState, string partition)
    {
        ObserverDeliveryScope delivery;
        try
        {
            delivery = ObserverDeliveryScope.Enter(serviceProvider, DeliveryContextFor(handler, @namespace));
        }
        catch (Exception enterException)
        {
            logger.FailedEnteringDeliveryScopeForReactorReplayNotification(enterException, handler.Id, replayState);
            return;
        }

        using var deliveryScope = delivery;
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
