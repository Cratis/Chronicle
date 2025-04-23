// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Jobs;
using Microsoft.Extensions.Logging;
using OneOf.Types;

namespace Cratis.Chronicle.Grains.Observation.Jobs;

/// <summary>
/// Represents a step in a replay job.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="HandleEventsForPartition"/> class.
/// </remarks>
/// <param name="state"><see cref="IPersistentState{TState}"/> for managing state of the job step.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing storage for the cluster.</param>
/// <param name="logger">The logger.</param>
public class HandleEventsForPartition(
    [PersistentState(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<HandleEventsForPartitionState> state,
    IStorage storage,
    ILogger<HandleEventsForPartition> logger) : JobStep<HandleEventsForPartitionArguments, HandleEventsForPartitionResult, HandleEventsForPartitionState>(state, logger), IHandleEventsForPartition
{
    const string SubscriberDisconnected = "Subscriber is disconnected";

    IEventSequenceStorage? _eventSequenceStorage;
    IObserver _observer = null!;
    EventSourceId _eventSourceId = EventSourceId.Unspecified;
    IObserverSubscriber? _subscriber;

    IHandleEventsForPartition _selfGrainReference = null!;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _selfGrainReference = this.AsReference<IHandleEventsForPartition>();

        if (State.Prepared)
        {
            _observer = GrainFactory.GetGrain<IObserver>(State.ObserverKey);
            var subscription = await _observer.GetSubscription();
            _eventSourceId = State.Partition.ToString();
            _subscriber = (GrainFactory.GetGrain(subscription.SubscriberType, GetObserverSubscriberKey(subscription)) as IObserverSubscriber)!;
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

    /// <inheritdoc/>
    protected override ValueTask InitializeState(HandleEventsForPartitionArguments request)
    {
        State.ObserverKey = request.ObserverKey;
        State.EventObservationState = request.EventObservationState;
        State.Partition = request.Partition;
        State.StartEventSequenceNumber = request.StartEventSequenceNumber;
        State.EndEventSequenceNumber = request.EndEventSequenceNumber;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    protected override ValueTask<HandleEventsForPartitionResult?> CreateCancelledResultFromCurrentState(HandleEventsForPartitionState currentState) =>
        ValueTask.FromResult<HandleEventsForPartitionResult?>(new(currentState.LastSuccessfullyHandledEventSequenceNumber));

    /// <inheritdoc/>
    protected override async Task<Concepts.Result<PrepareJobStepError>> PrepareStep(HandleEventsForPartitionArguments request)
    {
        try
        {
            logger.Preparing(request.Partition, request.StartEventSequenceNumber, request.EndEventSequenceNumber);
            _observer = GrainFactory.GetGrain<IObserver>(request.ObserverKey);
            var subscription = await _observer.GetSubscription();
            _eventSourceId = request.Partition.ToString() ?? EventSourceId.Unspecified;

            if (subscription.IsSubscribed)
            {
                _subscriber = (GrainFactory.GetGrain(subscription.SubscriberType, request.ToObserverSubscriberKey(subscription.SiloAddress)) as IObserverSubscriber)!;
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

            using var events = await eventSequenceStorage.GetRange(
                currentState.LastSuccessfullyHandledEventSequenceNumber == EventSequenceNumber.Unavailable
                    ? currentState.StartEventSequenceNumber
                    : currentState.LastSuccessfullyHandledEventSequenceNumber.Next(),
                currentState.EndEventSequenceNumber,
                _eventSourceId,
                subscription.EventTypes,
                cancellationToken);

            var subscriberContext = new ObserverSubscriberContext(subscription.Arguments);

            var failed = false;
            var exceptionMessages = Enumerable.Empty<string>().ToArray();
            var exceptionStackTrace = string.Empty;

            var lastEventSequenceNumberAttempted = EventSequenceNumber.Unavailable;
            while (await events.MoveNext())
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    LogCancelled(lastEventSequenceNumberAttempted, currentState.Partition);
                    return JobStepResult.Failed(PerformJobStepError.CancelledWithPartialResult(CreateResult(lastSuccessfullyHandledEventSequenceNumber)));
                }
                var handledCount = EventCount.Zero;

                var handleEventsResult = await TryHandleEvents(currentState, events, subscriberContext);
                if (handleEventsResult.TryGetException(out var handleEventsException))
                {
                    failed = true;
                    exceptionMessages = handleEventsException.GetAllMessages().ToArray();
                    exceptionStackTrace = handleEventsException.StackTrace ?? string.Empty;
                    lastEventSequenceNumberAttempted = lastSuccessfullyHandledEventSequenceNumber.Next();
                }
                else if (handleEventsResult.TryGetResult(out var handledEventsResult))
                {
                    var (eventObserverResult, handledEvents) = handledEventsResult;
                    if (eventObserverResult.LastSuccessfulObservation.IsActualValue)
                    {
                        handledCount = events.Current.Count(_ => _.Metadata.SequenceNumber <= eventObserverResult.LastSuccessfulObservation);
                    }
                    switch (eventObserverResult.State)
                    {
                        case ObserverSubscriberState.Ok:
                            logger.SuccessfullyHandledEvents(currentState.Partition, handledCount, eventObserverResult.LastSuccessfulObservation);
                            lastEventSequenceNumberAttempted = EventSequenceNumber.Unavailable;
                            await _selfGrainReference.ReportNewSuccessfullyHandledEvent(eventObserverResult.LastSuccessfulObservation);
                            lastSuccessfullyHandledEventSequenceNumber = eventObserverResult.LastSuccessfulObservation;
                            break;
                        case ObserverSubscriberState.Failed:
                            failed = true;
                            exceptionMessages = eventObserverResult.ExceptionMessages.ToArray();
                            exceptionStackTrace = eventObserverResult.ExceptionStackTrace;
                            lastEventSequenceNumberAttempted = eventObserverResult.HandledAnyEvents
                                ? handledEvents.First(e => e.Metadata.SequenceNumber > eventObserverResult.LastSuccessfulObservation).Metadata.SequenceNumber
                                : handledEvents[0].Metadata.SequenceNumber;
                            if (eventObserverResult.HandledAnyEvents)
                            {
                                await _selfGrainReference.ReportNewSuccessfullyHandledEvent(eventObserverResult.LastSuccessfulObservation);
                                lastSuccessfullyHandledEventSequenceNumber = eventObserverResult.LastSuccessfulObservation;
                            }

                            logger.FailedHandlingEvents(currentState.Partition, handledCount, lastEventSequenceNumberAttempted, lastSuccessfullyHandledEventSequenceNumber);
                            break;
                        case ObserverSubscriberState.Disconnected:
                            failed = true;
                            exceptionMessages = [SubscriberDisconnected];
                            lastEventSequenceNumberAttempted = lastSuccessfullyHandledEventSequenceNumber.Next();
                            logger.EventHandlerDisconnected(currentState.Partition, lastSuccessfullyHandledEventSequenceNumber);
                            break;
                    }
                }

                if (failed)
                {
                    var failedAt = lastEventSequenceNumberAttempted.IsActualValue ? lastEventSequenceNumberAttempted : currentState.StartEventSequenceNumber;
                    await _observer.PartitionFailed(_eventSourceId, failedAt, exceptionMessages, exceptionStackTrace);
                    return JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(CreateResult(lastSuccessfullyHandledEventSequenceNumber), exceptionMessages, exceptionStackTrace));
                }
            }

            if (lastSuccessfullyHandledEventSequenceNumber == EventSequenceNumber.Unavailable)
            {
                logger.HandledNoneEvents(currentState.Partition);
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
    static HandleEventsForPartitionResult CreateResult(EventSequenceNumber lastSuccessfullyHandled) => new(lastSuccessfullyHandled);

    async Task<Catch<(ObserverSubscriberResult Result, AppendedEvent[] HandledEvents), None>> TryHandleEvents(
        HandleEventsForPartitionState state,
        IEventCursor events,
        ObserverSubscriberContext subscriberContext)
    {
        try
        {
            var eventsToHandle = SetObservationStateIfSpecified(state.EventObservationState, events);
            if (eventsToHandle.Length != 0)
            {
                var result = await _subscriber!.OnNext(state.Partition, eventsToHandle, subscriberContext);
                return (result, eventsToHandle);
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

    ObserverSubscriberKey GetObserverSubscriberKey(ObserverSubscription subscription)
    {
        return new(
            State.ObserverKey.ObserverId,
            State.ObserverKey.EventStore,
            State.ObserverKey.Namespace,
            State.ObserverKey.EventSequenceId,
            State.Partition,
            subscription.SiloAddress.ToParsableString());
    }

    IEventSequenceStorage GetEventSequenceStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId) =>
        _eventSequenceStorage ??= storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(eventSequenceId);
}
