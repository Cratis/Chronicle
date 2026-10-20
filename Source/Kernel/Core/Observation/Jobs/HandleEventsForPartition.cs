// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Logging;
using OneOf.Types;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents a step in a replay job.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="HandleEventsForPartition"/> class.
/// </remarks>
/// <param name="state"><see cref="IPersistentState{TState}"/> for managing state of the job step.</param>
/// <param name="throttle">The <see cref="IJobStepThrottle"/> for limiting parallel execution.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing storage for the cluster.</param>
/// <param name="eventCompliance"><see cref="IEventCompliance"/> for decrypting PII event content before dispatching to subscribers.</param>
/// <param name="subscriberSelector"><see cref="IObserverSubscriberSelector"/> for selecting which connected client instance to deliver to.</param>
/// <param name="configurationProvider"><see cref="IConfigurationForObserverProvider"/> for getting the observer's subscriber timeout.</param>
/// <param name="logger">The logger.</param>
/// <param name="eventGenerationRelease">The pinned generation release boundary.</param>
public class HandleEventsForPartition(
    [PersistentState(nameof(JobStepState), Cratis.Orleans.WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<HandleEventsForPartitionState> state,
    IJobStepThrottle throttle,
    IStorage storage,
    IEventCompliance eventCompliance,
    IObserverSubscriberSelector subscriberSelector,
    IConfigurationForObserverProvider configurationProvider,
    ILogger<HandleEventsForPartition> logger,
    IEventGenerationRelease? eventGenerationRelease = null) : JobStep<HandleEventsForPartitionArguments, HandleEventsForPartitionResult, HandleEventsForPartitionState>(state, throttle, logger), IHandleEventsForPartition
{
    const string SubscriberDisconnected = "Subscriber is disconnected";

    IEventSequenceStorage? _eventSequenceStorage;
    IObserver _observer = null!;
    EventSourceId _eventSourceId = EventSourceId.Unspecified;
    IObserverSubscriber? _subscriber;
    Dictionary<EventType, EventTypeSchema> _eventTypeSchemas = [];
    IEventTypesStorage? _eventTypes;
    EventType[] _generationPins = [];
    EventStoreName _generationEventStore = EventStoreName.NotSet;
    EventGenerationDelivery _generationDelivery;
    bool _isCollapsingProjection;

    /// <summary>
    /// Gets or sets how long to wait before a handling attempt that timed out is made again. Exposed for testing.
    /// </summary>
    internal TimeSpan TransientRetryDelay { get; set; } = TransientTimeouts.DefaultRetryDelay;

    /// <inheritdoc/>
    /// <remarks>
    /// A projection that collapses several event sources onto one read model document is deliberately written out
    /// of order per document, so its sink write cannot be guarded on the read model's watermark and a redelivered
    /// batch would be applied twice. Checkpointing after every batch shrinks that window to the single batch the
    /// live delivery path already carries. It equalizes the window with the live path; it does not make the
    /// subscriber idempotent.
    /// </remarks>
    protected override bool CheckpointAfterEveryBatch => _isCollapsingProjection;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (State.IsPrepared)
        {
            _observer = GrainFactory.GetGrain<IObserver>(State.ObserverKey);
            var subscription = await _observer.GetSubscription();
            _eventSourceId = State.Partition.ToString();

            // An unsubscribed observer reports a placeholder subscriber type that is not a grain interface, so
            // resolving it throws and the step can never activate - it would fail and retry forever instead of
            // reaching PerformStep, which reports the disconnection the job knows how to handle.
            if (subscription.IsSubscribed)
            {
                _subscriber = GrainFactory.GetGrain(subscription.SubscriberType, GetObserverSubscriberKey(subscription, State.Partition)) as IObserverSubscriber;
                _isCollapsingProjection = subscription.IsCollapsingProjection;
            }
        }
        await base.OnActivateAsync(cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This is the activation-side entry point. The step's own background work never calls it through the grain
    /// reference; it reaches the activation through TryInvokeOnActivation instead.
    /// </remarks>
    public async Task ReportNewSuccessfullyHandledEvent(EventSequenceNumber lastHandledEventSequenceNumber)
    {
        using var scope = logger.BeginJobStepScope(State);
        State.LastSuccessfullyHandledEventSequenceNumber = lastHandledEventSequenceNumber;
        var writeStateResult = await WriteCheckpointDebounced();
        if (writeStateResult.TryGetException(out var error))
        {
            logger.FailedToPersistSuccessfullyHandledEvent(error, lastHandledEventSequenceNumber);
            writeStateResult.RethrowError();
        }
    }

    /// <inheritdoc/>
    protected override ValueTask InitializeState(HandleEventsForPartitionArguments request)
    {
        State.ObserverKey = request.ObserverKey;
        State.EventObservationState = request.EventObservationState;
        State.EventTypes = request.EventTypes.ToArray();
        State.ReaderScope = request.ReaderScope;
        State.Partition = request.Partition;
        State.StartEventSequenceNumber = request.StartEventSequenceNumber;
        State.EndEventSequenceNumber = request.EndEventSequenceNumber;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    protected override ValueTask<HandleEventsForPartitionResult?> CreateCancelledResultFromCurrentState(HandleEventsForPartitionState currentState) =>
        ValueTask.FromResult<HandleEventsForPartitionResult?>(new(currentState.LastSuccessfullyHandledEventSequenceNumber));

    /// <inheritdoc/>
    protected override async Task<Monads.Result<PrepareJobStepError>> PrepareStep(HandleEventsForPartitionArguments request)
    {
        try
        {
            logger.Preparing(request.Partition, request.StartEventSequenceNumber, request.EndEventSequenceNumber);
            _observer = GrainFactory.GetGrain<IObserver>(request.ObserverKey);
            var subscription = await _observer.GetSubscription();
            _eventSourceId = request.Partition.ToString() ?? EventSourceId.Unspecified;

            if (subscription.IsSubscribed)
            {
                _subscriber = GrainFactory.GetGrain(subscription.SubscriberType, GetObserverSubscriberKey(subscription, request.Partition)) as IObserverSubscriber;
                _isCollapsingProjection = subscription.IsCollapsingProjection;
                logger.SuccessfullyPrepared(request.Partition);
                return Result.Success<PrepareJobStepError>();
            }

            logger.PreparingStoppedUnsubscribed(request.Partition);
            return Result.Failed(PrepareJobStepError.CannotPrepare);
        }
        catch (Exception e)
        {
            logger.FailedPreparing(e, nameof(HandleEventsForPartition));
            return Result.Failed(PrepareJobStepError.UnexpectedErrorPreparing);
        }
    }

    /// <inheritdoc/>
    protected override async Task<Catch<JobStepResult>> PerformStep(HandleEventsForPartitionState currentState, CancellationToken cancellationToken)
    {
        var lastSuccessfullyHandledEventSequenceNumber = EventSequenceNumber.Unavailable;
        var subscription = await _observer.GetSubscription();
        _generationDelivery = subscription.GenerationDelivery;
        try
        {
            lastSuccessfullyHandledEventSequenceNumber = currentState.LastSuccessfullyHandledEventSequenceNumber;
            if (_subscriber is null || !subscription.IsSubscribed)
            {
                logger.PerformingStoppedUnsubscribed(currentState.Partition);
                return JobStepResult.Failed(SubscriberDisconnected, "This should have been ensured in the Prepare operation");
            }
            if (cancellationToken.IsCancellationRequested)
            {
                logger.CancelledBeforeHandlingAnyEvents(currentState.Partition);
                return JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult());
            }
            var eventSequenceStorage = GetEventSequenceStorage(
                currentState.ObserverKey.EventStore,
                currentState.ObserverKey.Namespace,
                currentState.ObserverKey.EventSequenceId);
            var scopedEventTypes = ObserverReaderScope
                .ForRequest(currentState.ReaderScope, currentState.EventTypes)
                .EventTypesToRead(currentState.EventTypes);
            var eventTypesToRead = scopedEventTypes?.ToArray()
                ?? await ResolveFallbackEventTypesToRead(subscription.EventTypes);
            _generationPins = eventTypesToRead.ToArray();
            _generationEventStore = currentState.ObserverKey.EventStore;
            var nonRedactionEventTypeIds = eventTypesToRead
                .Where(et => et.Id != GlobalEventTypes.Redaction)
                .Select(et => et.Id)
                .ToHashSet();
            _eventTypes = storage.GetEventStore(currentState.ObserverKey.EventStore).EventTypes;
            _eventTypeSchemas = (await _eventTypes.GetFor(eventTypesToRead))
                .ToDictionary(_ => _.Type);

            using var events = await eventSequenceStorage.GetRange(
                currentState.LastSuccessfullyHandledEventSequenceNumber == EventSequenceNumber.Unavailable
                    ? currentState.StartEventSequenceNumber
                    : currentState.LastSuccessfullyHandledEventSequenceNumber.Next(),
                currentState.EndEventSequenceNumber,
                _eventSourceId,
                eventTypesToRead,
                cancellationToken: cancellationToken);

            var subscriberContext = new ObserverSubscriberContext(
                subscriberSelector.Select(subscription, currentState.Partition).ConnectedClient ?? subscription.Arguments);
            var subscriberTimeout = await configurationProvider.GetSubscriberTimeoutForObserver(currentState.ObserverKey);

            var failed = false;
            var exceptionMessages = Enumerable.Empty<string>().ToArray();
            var exceptionStackTrace = string.Empty;
            var failureKind = FailureKind.Unknown;

            var lastEventSequenceNumberAttempted = EventSequenceNumber.Unavailable;
            var lastScannedEventSequenceNumber = EventSequenceNumber.Unavailable;
            while (await events.MoveNext())
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    LogCancelled(lastEventSequenceNumberAttempted, currentState.Partition);
                    return JobStepResult.Failed(PerformJobStepError.CancelledWithPartialResult(CreateResult(lastSuccessfullyHandledEventSequenceNumber)));
                }
                var handledCount = EventCount.Zero;

                var handleEventsResult = _subscriber is IWebhookObserverSubscriber
                    ? await TryHandleEvents(currentState, events, subscriberContext, subscriberTimeout, nonRedactionEventTypeIds, subscription.Filters, cancellationToken)
                    : await TransientTimeouts.RetryOnceOnTimeout(
                        () => TryHandleEvents(currentState, events, subscriberContext, subscriberTimeout, nonRedactionEventTypeIds, subscription.Filters, cancellationToken),
                        TransientRetryDelay,
                        timeout => logger.RetryingAfterTimeout(currentState.Partition, TransientTimeouts.ShortMessageFor(timeout)),
                        cancellationToken);
                if (_subscriber is IWebhookObserverSubscriber && cancellationToken.IsCancellationRequested)
                {
                    LogCancelled(lastSuccessfullyHandledEventSequenceNumber, currentState.Partition);
                    return JobStepResult.Failed(PerformJobStepError.CancelledWithPartialResult(CreateResult(lastSuccessfullyHandledEventSequenceNumber)));
                }

                if (handleEventsResult.TryGetException(out var handleEventsException))
                {
                    failed = true;
                    exceptionMessages = handleEventsException.GetAllMessages().ToArray();
                    exceptionStackTrace = handleEventsException.StackTrace ?? string.Empty;
                    failureKind = handleEventsException.ToFailureKind();
                    if (failureKind == FailureKind.Timeout)
                    {
                        exceptionMessages = [TransientTimeouts.ShortMessageFor(handleEventsException)];
                        exceptionStackTrace = string.Empty;
                    }

                    lastEventSequenceNumberAttempted = events.Current.First().Context.SequenceNumber;
                }
                else if (handleEventsResult.TryGetResult(out var handledEventsResult))
                {
                    var (eventObserverResult, handledEvents) = handledEventsResult;
                    if (eventObserverResult.LastSuccessfulObservation.IsActualValue)
                    {
                        handledCount = events.Current.Count(_ => _.Context.SequenceNumber <= eventObserverResult.LastSuccessfulObservation);
                    }
                    switch (eventObserverResult.State)
                    {
                        case ObserverSubscriberState.Ok:
                            lastEventSequenceNumberAttempted = EventSequenceNumber.Unavailable;
                            if (!await ReportFromWork(eventObserverResult.LastSuccessfulObservation))
                            {
                                // Nothing was recorded for this batch, so it is neither checkpointed nor counted here: the next
                                // run resumes from the last persisted checkpoint and counts it when it handles the batch again.
                                return ClosedResult(lastSuccessfullyHandledEventSequenceNumber);
                            }

                            lastSuccessfullyHandledEventSequenceNumber = eventObserverResult.LastSuccessfulObservation;
                            await ReportHandledEvents(currentState.Partition, handledEvents, eventObserverResult.LastSuccessfulObservation, cancellationToken);
                            break;
                        case ObserverSubscriberState.Failed:
                            failed = true;
                            exceptionMessages = eventObserverResult.ExceptionMessages.ToArray();
                            exceptionStackTrace = eventObserverResult.ExceptionStackTrace;
                            failureKind = FailureKind.Handling;
                            if (eventObserverResult.HandledAnyEvents)
                            {
                                var failedEvent = handledEvents.FirstOrDefault(e => e.Context.SequenceNumber > eventObserverResult.LastSuccessfulObservation);
                                lastEventSequenceNumberAttempted = failedEvent is not null
                                    ? failedEvent.Context.SequenceNumber
                                    : eventObserverResult.LastSuccessfulObservation.Next();

                                if (!await ReportFromWork(eventObserverResult.LastSuccessfulObservation))
                                {
                                    return ClosedResult(lastSuccessfullyHandledEventSequenceNumber);
                                }

                                lastSuccessfullyHandledEventSequenceNumber = eventObserverResult.LastSuccessfulObservation;
                                await ReportHandledEvents(currentState.Partition, handledEvents, eventObserverResult.LastSuccessfulObservation, cancellationToken);
                            }
                            else
                            {
                                lastEventSequenceNumberAttempted = handledEvents[0].Context.SequenceNumber;
                            }

                            logger.FailedHandlingEvents(currentState.Partition, handledCount, lastEventSequenceNumberAttempted, lastSuccessfullyHandledEventSequenceNumber);
                            break;
                        case ObserverSubscriberState.Disconnected:
                            failed = true;
                            exceptionMessages = [SubscriberDisconnected];
                            failureKind = FailureKind.Disconnected;
                            lastEventSequenceNumberAttempted = handledEvents[0].Context.SequenceNumber;
                            logger.EventHandlerDisconnected(currentState.Partition, lastSuccessfullyHandledEventSequenceNumber);
                            break;
                    }
                }

                if (failed)
                {
                    var failedAt = lastEventSequenceNumberAttempted.IsActualValue ? lastEventSequenceNumberAttempted : currentState.StartEventSequenceNumber;
                    if (failureKind == FailureKind.Timeout)
                    {
                        // The observer is not told yet: it is the grain that was too busy to answer, and the job runs the
                        // step again before it reports the partition.
                        logger.StepTimedOut(exceptionMessages[0]);
                        return JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
                            CreateResult(lastSuccessfullyHandledEventSequenceNumber) with
                            {
                                TransientFailure = new(currentState.Partition, failedAt, exceptionMessages)
                            },
                            exceptionMessages,
                            string.Empty));
                    }

                    await _observer.PartitionFailed(_eventSourceId, failedAt, exceptionMessages, exceptionStackTrace, failureKind);
                    return JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
                        CreateResult(lastSuccessfullyHandledEventSequenceNumber),
                        TransientTimeouts.ShortenForStep(exceptionMessages),
                        string.Empty));
                }

                lastScannedEventSequenceNumber = GetLastScanned(lastScannedEventSequenceNumber, events);
            }

            if (lastSuccessfullyHandledEventSequenceNumber == EventSequenceNumber.Unavailable)
            {
                using (logger.BeginObserverKeyScope(currentState.ObserverKey))
                {
                    logger.HandledNoEvents(currentState.Partition);
                }
            }
            else
            {
                logger.HandledAllEvents(currentState.Partition, lastSuccessfullyHandledEventSequenceNumber);
            }

            return JobStepResult.Succeeded(CreateResult(lastSuccessfullyHandledEventSequenceNumber) with
            {
                LastScannedEventSequenceNumber = lastScannedEventSequenceNumber
            });
        }
        catch (TaskCanceledException)
        {
            LogCancelled(lastSuccessfullyHandledEventSequenceNumber, currentState.Partition);
            return JobStepResult.Failed(PerformJobStepError.CancelledWithPartialResult(CreateResult(lastSuccessfullyHandledEventSequenceNumber)));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogCancelled(lastSuccessfullyHandledEventSequenceNumber, currentState.Partition);
            return JobStepResult.Failed(PerformJobStepError.CancelledWithPartialResult(CreateResult(lastSuccessfullyHandledEventSequenceNumber)));
        }
        catch (Exception e) when (TransientTimeouts.IsTimeout(e))
        {
            // A grain call timing out is recorded once, briefly, and never escalated to the observer: the observer
            // is the grain that was too busy to answer.
            var message = TransientTimeouts.ShortMessageFor(e);
            logger.StepTimedOut(message);
            return JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
                CreateResult(lastSuccessfullyHandledEventSequenceNumber) with
                {
                    TransientFailure = new(
                        currentState.Partition,
                        lastSuccessfullyHandledEventSequenceNumber.IsActualValue ? lastSuccessfullyHandledEventSequenceNumber.Next() : currentState.StartEventSequenceNumber,
                        [message])
                },
                [message],
                string.Empty));
        }
        catch (Exception e)
        {
            logger.FailedPerforming(e, nameof(HandleEventsForPartition));
            var failedAt = lastSuccessfullyHandledEventSequenceNumber.IsActualValue ? lastSuccessfullyHandledEventSequenceNumber.Next() : currentState.StartEventSequenceNumber;
            if (!await TryReportPartitionFailed(failedAt, e))
            {
                return UnreportedFailure(currentState.Partition, failedAt, lastSuccessfullyHandledEventSequenceNumber, e);
            }

            if (!lastSuccessfullyHandledEventSequenceNumber.IsActualValue)
            {
                return e;
            }

            logger.FailedWithPartialSuccess(e, lastSuccessfullyHandledEventSequenceNumber);
            return JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(CreateResult(lastSuccessfullyHandledEventSequenceNumber), e));
        }
    }

    /// <summary>
    /// Creates the result for a failure the observer could not be told about. It is handed to the job as transient, so
    /// the job runs the step again and reports the partition as failed when the attempts run out - a failure is never
    /// left without a failed partition to retry it.
    /// </summary>
    /// <param name="partition">The partition the step was handling.</param>
    /// <param name="failedAt">The sequence number the partition failed at.</param>
    /// <param name="lastSuccessfullyHandled">The last successfully handled event sequence number.</param>
    /// <param name="error">The failure.</param>
    /// <returns>The <see cref="JobStepResult"/>.</returns>
    static JobStepResult UnreportedFailure(Key partition, EventSequenceNumber failedAt, EventSequenceNumber lastSuccessfullyHandled, Exception error)
    {
        var messages = TransientTimeouts.ShortenForStep(error.GetAllMessages());
        return JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
            CreateResult(lastSuccessfullyHandled) with { TransientFailure = new(partition, failedAt, messages) },
            messages,
            string.Empty));
    }

    static AppendedEvent[] SetObservationStateIfSpecified(EventObservationState eventObservationState, IEventCursor events)
    {
        if (eventObservationState != EventObservationState.None)
        {
            return events.Current.Select(@event =>
                @event with
                {
                    Context = @event.Context with
                    {
                        ObservationState = eventObservationState
                    }
                }).ToArray();
        }

        return events.Current.ToArray();
    }

    static AppendedEvent[] FilterRedactedEventsForUnsubscribedTypes(AppendedEvent[] events, HashSet<EventTypeId> nonRedactionEventTypeIds)
    {
        if (nonRedactionEventTypeIds.Count == 0)
        {
            return events;
        }

        var filtered = new AppendedEvent[events.Length];
        var count = 0;
        foreach (var @event in events)
        {
            if (@event.Context.EventType.Id != GlobalEventTypes.Redaction)
            {
                filtered[count++] = @event;
                continue;
            }

            if (@event.Content is not IDictionary<string, object?> contentDict || !contentDict.TryGetValue("originalEventType", out var originalEventTypeObj))
            {
                continue;
            }

            var originalEventTypeId = originalEventTypeObj?.ToString();
            if (originalEventTypeId is not null && nonRedactionEventTypeIds.Contains(new EventTypeId(originalEventTypeId)))
            {
                filtered[count++] = @event;
            }
        }

        return count == filtered.Length ? filtered : filtered[..count];
    }

    static HandleEventsForPartitionResult CreateResult(EventSequenceNumber lastSuccessfullyHandled) => new(lastSuccessfullyHandled);

    static EventSequenceNumber GetLastScanned(EventSequenceNumber lastScanned, IEventCursor events)
    {
        foreach (var @event in events.Current)
        {
            if (!lastScanned.IsActualValue || @event.Context.SequenceNumber > lastScanned)
            {
                lastScanned = @event.Context.SequenceNumber;
            }
        }

        return lastScanned;
    }

    async Task<bool> TryReportPartitionFailed(EventSequenceNumber failedAt, Exception error)
    {
        try
        {
            await _observer.PartitionFailed(_eventSourceId, failedAt, error.GetAllMessages(), error.StackTrace ?? string.Empty, error.ToFailureKind());
            return true;
        }
        catch (Exception reportError)
        {
            logger.FailedReportingPartitionFailed(reportError, _eventSourceId, failedAt);
            return false;
        }
    }

    /// <summary>
    /// Reports progress from the step's background work. The state update and the debounced write run as one
    /// callback on the activation, so a stopped or deactivating step either records the whole batch or none of it.
    /// </summary>
    /// <param name="lastHandled">The last handled event sequence number.</param>
    /// <returns>True when the progress was recorded; false when the step refused the callback because its run is closed.</returns>
    async Task<bool> ReportFromWork(EventSequenceNumber lastHandled)
    {
        var reported = await TryInvokeOnActivation(() => ReportNewSuccessfullyHandledEvent(lastHandled));
        if (reported.TryGetError(out var rejection))
        {
            logger.CheckpointCallbackRejected(_eventSourceId, rejection, lastHandled);
            return false;
        }

        return true;
    }

    JobStepResult ClosedResult(EventSequenceNumber lastSuccessfullyHandled) =>
        JobStepResult.Failed(PerformJobStepError.CancelledWithPartialResult(CreateResult(lastSuccessfullyHandled)));

    async Task<Catch<(ObserverSubscriberResult Result, AppendedEvent[] HandledEvents), None>> TryHandleEvents(
        HandleEventsForPartitionState state,
        IEventCursor events,
        ObserverSubscriberContext subscriberContext,
        TimeSpan subscriberTimeout,
        HashSet<EventTypeId> nonRedactionEventTypeIds,
        ObserverFilters? filters,
        CancellationToken cancellationToken)
    {
        try
        {
            var eventsToHandle = SetObservationStateIfSpecified(state.EventObservationState, events);
            eventsToHandle = FilterRedactedEventsForUnsubscribedTypes(eventsToHandle, nonRedactionEventTypeIds);
            eventsToHandle = filters.Apply(eventsToHandle);
            if (eventsToHandle.Length != 0)
            {
                var decryptedEvents = await DecryptEvents(eventsToHandle);
                var deliveryCancellation = _subscriber is IWebhookObserverSubscriber ? cancellationToken : CancellationToken.None;
                var result = await _subscriber!.OnNextWithin(subscriberTimeout, state.Partition, decryptedEvents, subscriberContext, deliveryCancellation);
                return (result, decryptedEvents);
            }

            logger.NoMoreEventsToHandle(state.Partition, state.StartEventSequenceNumber, state.EndEventSequenceNumber);
            return default(None);
        }
        catch (Exception ex)
        {
            logger.ErrorHandling(ex, state.Partition);
            return ex;
        }
    }

    void LogCancelled(EventSequenceNumber lastHandledSequenceNumber, Key partition)
    {
        if (!lastHandledSequenceNumber.IsActualValue)
        {
            logger.CancelledBeforeHandlingAnyEvents(partition);
        }
        else
        {
            logger.CancelledAfterHandlingEvents(partition, lastHandledSequenceNumber);
        }
    }

    ObserverSubscriberKey GetObserverSubscriberKey(ObserverSubscription subscription, Key partition)
    {
        var siloAddress = typeof(IWebhookObserverSubscriber).IsAssignableFrom(subscription.SubscriberType)
            ? SiloAddress.Zero
            : subscriberSelector.Select(subscription, partition).SiloAddress;
        return subscription.GetSubscriberKeyFor(partition, siloAddress);
    }

    IEventSequenceStorage GetEventSequenceStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId) =>
        _eventSequenceStorage ??= storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(eventSequenceId);

    async Task<AppendedEvent[]> DecryptEvents(IEnumerable<AppendedEvent> events)
    {
        if (_generationDelivery == EventGenerationDelivery.Pinned)
        {
            return await (eventGenerationRelease ?? throw new EventGenerationDeliveryNotSupported()).Release(_generationEventStore, _generationPins, _eventTypeSchemas, events);
        }

        // The schemas read up front are those of the generations the observer subscribes to, while the events carry the
        // generation they were stored at - the schema of that generation is what says what to decrypt.
        var eventsToDecrypt = events as AppendedEvent[] ?? events.ToArray();
        if (_eventTypes is not null)
        {
            await _eventTypes.EnsureSchemasFor(_eventTypeSchemas, eventsToDecrypt);
        }

        return await eventCompliance.Release(eventsToDecrypt, _eventTypeSchemas);
    }

    /// <summary>
    /// Resolve the event types to read when the observer's subscription itself carries none - an observer
    /// subscribed to all events has no fixed list, since new event types can be registered after it subscribed,
    /// so its full, current set is resolved from the observer rather than trusted from the subscription snapshot.
    /// </summary>
    /// <param name="subscriptionEventTypes">The event types recorded on the current subscription.</param>
    /// <returns>The event types to read.</returns>
    async Task<EventType[]> ResolveFallbackEventTypesToRead(IEnumerable<EventType> subscriptionEventTypes)
    {
        var eventTypes = subscriptionEventTypes.ToArray();
        return eventTypes.Length != 0 ? eventTypes : (await _observer.GetEventTypes()).ToArray();
    }

    Task ReportHandledEvents(Key partition, AppendedEvent[] handledEvents, EventSequenceNumber handledUpTo, CancellationToken cancellationToken)
    {
        var counts = handledEvents
            .Where(e => e.Context.SequenceNumber <= handledUpTo)
            .CountByEventType();
        return TransientTimeouts.ReportHandledEventsWithoutFailing(
            _observer,
            new(partition, counts, JobStepId, handledUpTo),
            TransientRetryDelay,
            logger,
            cancellationToken);
    }
}
