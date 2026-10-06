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
public class HandleEventsForPartition(
    [PersistentState(nameof(JobStepState), Cratis.Orleans.WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<HandleEventsForPartitionState> state,
    IJobStepThrottle throttle,
    IStorage storage,
    IEventCompliance eventCompliance,
    IObserverSubscriberSelector subscriberSelector,
    IConfigurationForObserverProvider configurationProvider,
    ILogger<HandleEventsForPartition> logger) : JobStep<HandleEventsForPartitionArguments, HandleEventsForPartitionResult, HandleEventsForPartitionState>(state, throttle, logger), IHandleEventsForPartition
{
    const string SubscriberDisconnected = "Subscriber is disconnected";

    IEventSequenceStorage? _eventSequenceStorage;
    IObserver _observer = null!;
    EventSourceId _eventSourceId = EventSourceId.Unspecified;
    IObserverSubscriber? _subscriber;
    Dictionary<EventType, EventTypeSchema> _eventTypeSchemas = [];
    IEventTypesStorage? _eventTypes;
    bool _isCollapsingProjection;

    IHandleEventsForPartition _selfGrainReference = null!;

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
        _selfGrainReference = GetSelfGrainReference();

        if (State.IsPrepared)
        {
            _observer = GrainFactory.GetGrain<IObserver>(State.ObserverKey);
            var subscription = await _observer.GetSubscription();
            _eventSourceId = State.Partition.ToString();
            _subscriber = GrainFactory.GetGrain(subscription.SubscriberType, GetObserverSubscriberKey(subscription, State.Partition)) as IObserverSubscriber;
            _isCollapsingProjection = subscription.IsCollapsingProjection;
        }
        await base.OnActivateAsync(cancellationToken);
    }

    /// <inheritdoc/>
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

    /// <summary>
    /// Gets the self grain reference for this grain instance.
    /// </summary>
    /// <returns>The <see cref="IHandleEventsForPartition"/> grain reference.</returns>
    protected virtual IHandleEventsForPartition GetSelfGrainReference() => this.AsReference<IHandleEventsForPartition>();

    /// <inheritdoc/>
    protected override ValueTask InitializeState(HandleEventsForPartitionArguments request)
    {
        State.ObserverKey = request.ObserverKey;
        State.EventObservationState = request.EventObservationState;
        State.EventTypes = request.EventTypes.ToArray();
        State.Partition = request.Partition;
        State.StartEventSequenceNumber = request.StartEventSequenceNumber;
        State.EndEventSequenceNumber = request.EndEventSequenceNumber;
        State.ConcludesPartitionCatchUp = request.ConcludesPartitionCatchUp;
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
            var requestedEventTypes = currentState.EventTypes.ToArray();
            var eventTypesToRead = requestedEventTypes.Length != 0
                ? requestedEventTypes
                : await ResolveFallbackEventTypesToRead(subscription.EventTypes);
            var nonRedactionEventTypeIds = eventTypesToRead
                .Where(et => et.Id != GlobalEventTypes.Redaction)
                .Select(et => et.Id)
                .ToHashSet();
            _eventTypes = storage.GetEventStore(currentState.ObserverKey.EventStore).EventTypes;
            _eventTypeSchemas = (await _eventTypes.GetFor(eventTypesToRead))
                .ToDictionary(_ => _.Type);

            // Catch-up holds live delivery back for this partition until the whole job has reported back, so an event
            // appended after the cursor below has run dry is not delivered live. Tell the observer how far this step has
            // read, and read on from there for as long as the observer still finds events the step has not read. The
            // position is the next event after the last one read - not the last one handled - so a tail of events the
            // step reads but does not deliver, such as redactions of types the observer does not subscribe to, does not
            // look unread forever.
            var nextToRead = lastSuccessfullyHandledEventSequenceNumber == EventSequenceNumber.Unavailable
                ? currentState.StartEventSequenceNumber
                : lastSuccessfullyHandledEventSequenceNumber.Next();
            var concludingAttempted = false;
            while (true)
            {
                var nextToReadBeforeReading = nextToRead;
                using var events = await eventSequenceStorage.GetRange(
                    nextToRead,
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
                while (await events.MoveNext())
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        LogCancelled(lastEventSequenceNumberAttempted, currentState.Partition);
                        return JobStepResult.Failed(PerformJobStepError.CancelledWithPartialResult(CreateResult(lastSuccessfullyHandledEventSequenceNumber)));
                    }
                    var handledCount = EventCount.Zero;

                    var handleEventsResult = await TryHandleEvents(currentState, events, subscriberContext, subscriberTimeout, nonRedactionEventTypeIds);
                    if (handleEventsResult.TryGetException(out var handleEventsException))
                    {
                        failed = true;
                        exceptionMessages = handleEventsException.GetAllMessages().ToArray();
                        exceptionStackTrace = handleEventsException.StackTrace ?? string.Empty;
                        failureKind = handleEventsException.ToFailureKind();
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
                                await _selfGrainReference.ReportNewSuccessfullyHandledEvent(eventObserverResult.LastSuccessfulObservation);
                                lastSuccessfullyHandledEventSequenceNumber = eventObserverResult.LastSuccessfulObservation;
                                var okCountsPerEventType = handledEvents
                                    .Where(e => e.Context.SequenceNumber <= eventObserverResult.LastSuccessfulObservation)
                                    .CountByEventType();
                                await _observer.ReportHandledEvents(currentState.Partition, okCountsPerEventType);
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

                                    await _selfGrainReference.ReportNewSuccessfullyHandledEvent(eventObserverResult.LastSuccessfulObservation);
                                    lastSuccessfullyHandledEventSequenceNumber = eventObserverResult.LastSuccessfulObservation;
                                    var failedCountsPerEventType = handledEvents
                                        .Where(e => e.Context.SequenceNumber <= eventObserverResult.LastSuccessfulObservation)
                                        .CountByEventType();
                                    await _observer.ReportHandledEvents(currentState.Partition, failedCountsPerEventType);
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
                        await _observer.PartitionFailed(_eventSourceId, failedAt, exceptionMessages, exceptionStackTrace, failureKind);
                        return JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(CreateResult(lastSuccessfullyHandledEventSequenceNumber), exceptionMessages, exceptionStackTrace));
                    }

                    // Every event in the batch has now been read, whether it was delivered or filtered out.
                    var lastRead = events.Current.LastOrDefault()?.Context.SequenceNumber;
                    if (lastRead?.IsActualValue == true && lastRead.Next() > nextToRead)
                    {
                        nextToRead = lastRead.Next();
                    }
                }

                if (!currentState.ConcludesPartitionCatchUp)
                {
                    break;
                }

                // Reading on read nothing the observer was waiting for, so asking again would only spin. The observer
                // already knows how far this step read, and reads what was left behind once the job reports back.
                if (concludingAttempted && nextToRead == nextToReadBeforeReading)
                {
                    logger.CouldNotConcludePartitionCatchUp(currentState.Partition, nextToRead);
                    break;
                }

                if (await _observer.ConcludePartitionCatchUp(currentState.Partition, nextToRead, eventTypesToRead))
                {
                    break;
                }

                concludingAttempted = true;
                logger.ReadingOnForEventsAppendedWhileCatchingUp(currentState.Partition, nextToRead);
            }

            if (lastSuccessfullyHandledEventSequenceNumber == EventSequenceNumber.Unavailable)
            {
                logger.HandledNoEvents(currentState.Partition);
            }
            else
            {
                logger.HandledAllEvents(currentState.Partition, lastSuccessfullyHandledEventSequenceNumber);
            }

            return JobStepResult.Succeeded(CreateResult(lastSuccessfullyHandledEventSequenceNumber));
        }
        catch (TaskCanceledException)
        {
            LogCancelled(lastSuccessfullyHandledEventSequenceNumber, currentState.Partition);
            return JobStepResult.Failed(PerformJobStepError.CancelledWithPartialResult(CreateResult(lastSuccessfullyHandledEventSequenceNumber)));
        }
        catch (Exception e)
        {
            logger.FailedPerforming(e, nameof(HandleEventsForPartition));
            if (!lastSuccessfullyHandledEventSequenceNumber.IsActualValue)
            {
                return e;
            }

            logger.FailedWithPartialSuccess(e, lastSuccessfullyHandledEventSequenceNumber);
            return JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(CreateResult(lastSuccessfullyHandledEventSequenceNumber), e));
        }
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

    async Task<Catch<(ObserverSubscriberResult Result, AppendedEvent[] HandledEvents), None>> TryHandleEvents(
        HandleEventsForPartitionState state,
        IEventCursor events,
        ObserverSubscriberContext subscriberContext,
        TimeSpan subscriberTimeout,
        HashSet<EventTypeId> nonRedactionEventTypeIds)
    {
        try
        {
            var eventsToHandle = SetObservationStateIfSpecified(state.EventObservationState, events);
            eventsToHandle = FilterRedactedEventsForUnsubscribedTypes(eventsToHandle, nonRedactionEventTypeIds);
            if (eventsToHandle.Length != 0)
            {
                var decryptedEvents = await DecryptEvents(eventsToHandle);
                var result = await _subscriber!.OnNextWithin(subscriberTimeout, state.Partition, decryptedEvents, subscriberContext);
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

    ObserverSubscriberKey GetObserverSubscriberKey(ObserverSubscription subscription, Key partition) =>
        subscription.GetSubscriberKeyFor(partition, subscriberSelector.Select(subscription, partition).SiloAddress);

    IEventSequenceStorage GetEventSequenceStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId) =>
        _eventSequenceStorage ??= storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(eventSequenceId);

    async Task<AppendedEvent[]> DecryptEvents(IEnumerable<AppendedEvent> events)
    {
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
}
