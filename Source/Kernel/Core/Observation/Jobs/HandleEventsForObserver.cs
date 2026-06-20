// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Monads;
using Microsoft.Extensions.Logging;
using OneOf.Types;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents a step in a replay job that handles events in global event-sequence order.
/// </summary>
/// <param name="state"><see cref="IPersistentState{TState}"/> for managing state of the job step.</param>
/// <param name="throttle">The <see cref="IJobStepThrottle"/> for limiting parallel execution.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing storage for the cluster.</param>
/// <param name="eventCompliance"><see cref="IEventCompliance"/> for decrypting PII event content before dispatching to subscribers.</param>
/// <param name="logger">The logger.</param>
public class HandleEventsForObserver(
    [PersistentState(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<HandleEventsForObserverState> state,
    IJobStepThrottle throttle,
    IStorage storage,
    IEventCompliance eventCompliance,
    ILogger<HandleEventsForObserver> logger) : JobStep<HandleEventsForObserverArguments, HandleEventsForPartitionResult, HandleEventsForObserverState>(state, throttle, logger), IHandleEventsForObserver
{
    const string SubscriberDisconnected = "Subscriber is disconnected";

    IEventSequenceStorage? _eventSequenceStorage;
    IObserver _observer = null!;
    ObserverSubscription _subscription = ObserverSubscription.Unsubscribed;
    Dictionary<EventType, EventTypeSchema> _eventTypeSchemas = [];

    IHandleEventsForObserver _selfGrainReference = null!;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _selfGrainReference = GetSelfGrainReference();

        if (State.IsPrepared)
        {
            _observer = GrainFactory.GetGrain<IObserver>(State.ObserverKey);
            _subscription = await _observer.GetSubscription();
        }
        await base.OnActivateAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ReportNewSuccessfullyHandledEvent(EventSequenceNumber lastHandledEventSequenceNumber)
    {
        using var scope = logger.BeginJobStepScope(State);
        State.LastSuccessfullyHandledEventSequenceNumber = lastHandledEventSequenceNumber;
        var writeStateResult = await WriteStateAsync();
        if (writeStateResult.TryGetException(out var error))
        {
            logger.FailedToPersistSuccessfullyHandledEvent(error, lastHandledEventSequenceNumber);
            writeStateResult.RethrowError();
        }
    }

    /// <summary>
    /// Gets the self grain reference for this grain instance.
    /// </summary>
    /// <returns>The <see cref="IHandleEventsForObserver"/> grain reference.</returns>
    protected virtual IHandleEventsForObserver GetSelfGrainReference() => this.AsReference<IHandleEventsForObserver>();

    /// <inheritdoc/>
    protected override ValueTask InitializeState(HandleEventsForObserverArguments request)
    {
        State.ObserverKey = request.ObserverKey;
        State.EventObservationState = request.EventObservationState;
        State.EventTypes = request.EventTypes.ToArray();
        State.StartEventSequenceNumber = request.StartEventSequenceNumber;
        State.EndEventSequenceNumber = request.EndEventSequenceNumber;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    protected override ValueTask<HandleEventsForPartitionResult?> CreateCancelledResultFromCurrentState(HandleEventsForObserverState currentState) =>
        ValueTask.FromResult<HandleEventsForPartitionResult?>(new(currentState.LastSuccessfullyHandledEventSequenceNumber));

    /// <inheritdoc/>
    protected override async Task<Monads.Result<PrepareJobStepError>> PrepareStep(HandleEventsForObserverArguments request)
    {
        try
        {
            logger.Preparing(request.StartEventSequenceNumber, request.EndEventSequenceNumber);
            _observer = GrainFactory.GetGrain<IObserver>(request.ObserverKey);
            _subscription = await _observer.GetSubscription();

            if (_subscription.IsSubscribed)
            {
                logger.SuccessfullyPrepared();
                return Result.Success<PrepareJobStepError>();
            }

            logger.PreparingStoppedUnsubscribed();
            return Result.Failed(PrepareJobStepError.CannotPrepare);
        }
        catch (Exception e)
        {
            logger.FailedPreparing(e, nameof(HandleEventsForObserver));
            return Result.Failed(PrepareJobStepError.UnexpectedErrorPreparing);
        }
    }

    /// <inheritdoc/>
    protected override async Task<Catch<JobStepResult>> PerformStep(HandleEventsForObserverState currentState, CancellationToken cancellationToken)
    {
        var lastSuccessfullyHandledEventSequenceNumber = EventSequenceNumber.Unavailable;
        _subscription = await _observer.GetSubscription();
        try
        {
            lastSuccessfullyHandledEventSequenceNumber = currentState.LastSuccessfullyHandledEventSequenceNumber;
            if (!_subscription.IsSubscribed)
            {
                logger.PerformingStoppedUnsubscribed();
                return JobStepResult.Failed(SubscriberDisconnected, "This should have been ensured in the Prepare operation");
            }
            if (cancellationToken.IsCancellationRequested)
            {
                logger.CancelledBeforeHandlingAnyEvents();
                return JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult());
            }
            var eventSequenceStorage = GetEventSequenceStorage(
                currentState.ObserverKey.EventStore,
                currentState.ObserverKey.Namespace,
                currentState.ObserverKey.EventSequenceId);
            var requestedEventTypes = currentState.EventTypes.ToArray();
            var eventTypesToRead = requestedEventTypes.Length != 0
                ? requestedEventTypes
                : _subscription.EventTypes.ToArray();
            var nonRedactionEventTypeIds = eventTypesToRead
                .Where(et => et.Id != GlobalEventTypes.Redaction)
                .Select(et => et.Id)
                .ToHashSet();
            _eventTypeSchemas = (await storage.GetEventStore(currentState.ObserverKey.EventStore).EventTypes.GetFor(eventTypesToRead))
                .ToDictionary(_ => _.Type);

            using var events = await eventSequenceStorage.GetRange(
                currentState.LastSuccessfullyHandledEventSequenceNumber == EventSequenceNumber.Unavailable
                    ? currentState.StartEventSequenceNumber
                    : currentState.LastSuccessfullyHandledEventSequenceNumber.Next(),
                currentState.EndEventSequenceNumber,
                eventTypes: eventTypesToRead,
                cancellationToken: cancellationToken);

            var subscriberContext = new ObserverSubscriberContext(_subscription.Arguments);
            var lastEventSequenceNumberAttempted = EventSequenceNumber.Unavailable;
            while (await events.MoveNext())
            {
                var eventsToHandle = SetObservationStateIfSpecified(currentState.EventObservationState, events)
                    .OrderBy(_ => _.Context.SequenceNumber)
                    .ToArray();
                eventsToHandle = FilterRedactedEventsForUnsubscribedTypes(eventsToHandle, nonRedactionEventTypeIds);

                foreach (var partitionEvents in GetConsecutivePartitionBatches(eventsToHandle))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        LogCancelled(lastSuccessfullyHandledEventSequenceNumber);
                        return JobStepResult.Failed(PerformJobStepError.CancelledWithPartialResult(CreateResult(lastSuccessfullyHandledEventSequenceNumber)));
                    }

                    var partition = partitionEvents[0].Context.EventSourceId;
                    var handleEventsResult = await TryHandleEvents(partition, partitionEvents, subscriberContext);
                    if (handleEventsResult.TryGetException(out var handleEventsException))
                    {
                        var exceptionMessages = handleEventsException.GetAllMessages().ToArray();
                        var exceptionStackTrace = handleEventsException.StackTrace ?? string.Empty;
                        lastEventSequenceNumberAttempted = partitionEvents[0].Context.SequenceNumber;
                        await _observer.PartitionFailed(partition, lastEventSequenceNumberAttempted, exceptionMessages, exceptionStackTrace);
                        return JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(CreateResult(lastSuccessfullyHandledEventSequenceNumber), exceptionMessages, exceptionStackTrace));
                    }
                    if (handleEventsResult.TryGetResult(out var handledEventsResult))
                    {
                        var (eventObserverResult, handledEvents) = handledEventsResult;
                        var (result, newLastSuccessfullyHandledEventSequenceNumber) = await HandleSubscriberResult(
                            currentState,
                            partition,
                            eventObserverResult,
                            handledEvents,
                            lastSuccessfullyHandledEventSequenceNumber);
                        lastSuccessfullyHandledEventSequenceNumber = newLastSuccessfullyHandledEventSequenceNumber;
                        if (result is not null)
                        {
                            return result;
                        }
                    }
                }
            }

            if (lastSuccessfullyHandledEventSequenceNumber == EventSequenceNumber.Unavailable)
            {
                logger.HandledNoEvents();
            }
            else
            {
                logger.HandledAllEvents(lastSuccessfullyHandledEventSequenceNumber);
            }

            return JobStepResult.Succeeded(CreateResult(lastSuccessfullyHandledEventSequenceNumber));
        }
        catch (TaskCanceledException)
        {
            LogCancelled(lastSuccessfullyHandledEventSequenceNumber);
            return JobStepResult.Failed(PerformJobStepError.CancelledWithPartialResult(CreateResult(lastSuccessfullyHandledEventSequenceNumber)));
        }
        catch (Exception e)
        {
            logger.FailedPerforming(e, nameof(HandleEventsForObserver));
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

    static IEnumerable<AppendedEvent[]> GetConsecutivePartitionBatches(AppendedEvent[] events)
    {
        var index = 0;
        while (index < events.Length)
        {
            var partition = events[index].Context.EventSourceId;
            var start = index;
            while (index < events.Length && events[index].Context.EventSourceId == partition)
            {
                index++;
            }

            yield return events[start..index];
        }
    }

    static HandleEventsForPartitionResult CreateResult(EventSequenceNumber lastSuccessfullyHandled) => new(lastSuccessfullyHandled);

    async Task<(JobStepResult? Result, EventSequenceNumber LastSuccessfullyHandledEventSequenceNumber)> HandleSubscriberResult(
        HandleEventsForObserverState currentState,
        Key partition,
        ObserverSubscriberResult eventObserverResult,
        AppendedEvent[] handledEvents,
        EventSequenceNumber lastSuccessfullyHandledEventSequenceNumber)
    {
        var handledCount = EventCount.Zero;
        if (eventObserverResult.LastSuccessfulObservation.IsActualValue)
        {
            handledCount = handledEvents.Count(_ => _.Context.SequenceNumber <= eventObserverResult.LastSuccessfulObservation);
        }

        switch (eventObserverResult.State)
        {
            case ObserverSubscriberState.Ok:
                await _selfGrainReference.ReportNewSuccessfullyHandledEvent(eventObserverResult.LastSuccessfulObservation);
                var okHandledEvents = handledEvents.Where(e => e.Context.SequenceNumber <= eventObserverResult.LastSuccessfulObservation).ToArray();
                await _observer.ReportHandledEvents(partition, okHandledEvents);
                return (null, eventObserverResult.LastSuccessfulObservation);
            case ObserverSubscriberState.Failed:
                return await HandleFailedSubscriberResult(
                    currentState,
                    partition,
                    eventObserverResult,
                    handledEvents,
                    handledCount,
                    lastSuccessfullyHandledEventSequenceNumber);
            case ObserverSubscriberState.Disconnected:
                var lastEventSequenceNumberAttempted = handledEvents[0].Context.SequenceNumber;
                logger.EventHandlerDisconnected(partition, lastSuccessfullyHandledEventSequenceNumber);
                await _observer.PartitionFailed(partition, lastEventSequenceNumberAttempted, [SubscriberDisconnected], string.Empty);
                return (
                    JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
                        CreateResult(lastSuccessfullyHandledEventSequenceNumber),
                        [SubscriberDisconnected],
                        string.Empty)),
                    lastSuccessfullyHandledEventSequenceNumber);
            default:
                return (null, lastSuccessfullyHandledEventSequenceNumber);
        }
    }

    async Task<(JobStepResult Result, EventSequenceNumber LastSuccessfullyHandledEventSequenceNumber)> HandleFailedSubscriberResult(
        HandleEventsForObserverState currentState,
        Key partition,
        ObserverSubscriberResult eventObserverResult,
        AppendedEvent[] handledEvents,
        EventCount handledCount,
        EventSequenceNumber lastSuccessfullyHandledEventSequenceNumber)
    {
        var lastEventSequenceNumberAttempted = EventSequenceNumber.Unavailable;
        if (eventObserverResult.HandledAnyEvents)
        {
            var failedEvent = handledEvents.FirstOrDefault(e => e.Context.SequenceNumber > eventObserverResult.LastSuccessfulObservation);
            lastEventSequenceNumberAttempted = failedEvent is not null
                ? failedEvent.Context.SequenceNumber
                : eventObserverResult.LastSuccessfulObservation.Next();

            await _selfGrainReference.ReportNewSuccessfullyHandledEvent(eventObserverResult.LastSuccessfulObservation);
            lastSuccessfullyHandledEventSequenceNumber = eventObserverResult.LastSuccessfulObservation;
            var failedHandledEvents = handledEvents.Where(e => e.Context.SequenceNumber <= eventObserverResult.LastSuccessfulObservation).ToArray();
            await _observer.ReportHandledEvents(partition, failedHandledEvents);
        }
        else
        {
            lastEventSequenceNumberAttempted = handledEvents[0].Context.SequenceNumber;
        }

        logger.FailedHandlingEvents(partition, handledCount, lastEventSequenceNumberAttempted, lastSuccessfullyHandledEventSequenceNumber);
        var failedAt = lastEventSequenceNumberAttempted.IsActualValue ? lastEventSequenceNumberAttempted : currentState.StartEventSequenceNumber;
        await _observer.PartitionFailed(partition, failedAt, eventObserverResult.ExceptionMessages, eventObserverResult.ExceptionStackTrace);
        return (
            JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
                CreateResult(lastSuccessfullyHandledEventSequenceNumber),
                eventObserverResult.ExceptionMessages,
                eventObserverResult.ExceptionStackTrace)),
            lastSuccessfullyHandledEventSequenceNumber);
    }

    async Task<Catch<(ObserverSubscriberResult Result, AppendedEvent[] HandledEvents), None>> TryHandleEvents(
        Key partition,
        AppendedEvent[] events,
        ObserverSubscriberContext subscriberContext)
    {
        try
        {
            var decryptedEvents = await DecryptEvents(events);
            var subscriber = GrainFactory.GetGrain(_subscription.SubscriberType, GetObserverSubscriberKey(partition)) as IObserverSubscriber;
            var result = await subscriber!.OnNext(partition, decryptedEvents, subscriberContext);
            return (result, decryptedEvents);
        }
        catch (Exception ex)
        {
            logger.ErrorHandling(ex, partition);
            return ex;
        }
    }

    void LogCancelled(EventSequenceNumber lastHandledSequenceNumber)
    {
        if (!lastHandledSequenceNumber.IsActualValue)
        {
            logger.CancelledBeforeHandlingAnyEvents();
        }
        else
        {
            logger.CancelledAfterHandlingEvents(lastHandledSequenceNumber);
        }
    }

    ObserverSubscriberKey GetObserverSubscriberKey(Key partition)
    {
        return new(
            State.ObserverKey.ObserverId,
            State.ObserverKey.EventStore,
            State.ObserverKey.Namespace,
            State.ObserverKey.EventSequenceId,
            partition,
            _subscription.SiloAddress.ToParsableString());
    }

    IEventSequenceStorage GetEventSequenceStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId) =>
        _eventSequenceStorage ??= storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(eventSequenceId);

    Task<AppendedEvent[]> DecryptEvents(IEnumerable<AppendedEvent> events) =>
        eventCompliance.Release(events, _eventTypeSchemas);
}

