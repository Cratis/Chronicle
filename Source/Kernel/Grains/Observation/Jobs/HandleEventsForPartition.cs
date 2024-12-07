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

    /// <inheritdoc/>
    public override Task<Concepts.Result<JobStepPrepareStartError>> Prepare(HandleEventsForPartitionArguments request)
    {
        using var scope = logger.BeginObserverScope(request.ObserverKey, State.Id.JobId, State.Id.JobStepId);
        try
        {
            logger.Preparing(request.Partition, request.StartEventSequenceNumber, request.EndEventSequenceNumber);
            _observer = GrainFactory.GetGrain<IObserver>(request.ObserverKey);
            _eventSourceId = request.Partition.ToString() ?? EventSourceId.Unspecified;

            if (request.ObserverSubscription.IsSubscribed)
            {
                var key = new ObserverSubscriberKey(
                    request.ObserverKey.ObserverId,
                    request.ObserverKey.EventStore,
                    request.ObserverKey.Namespace,
                    request.ObserverKey.EventSequenceId,
                    _eventSourceId,
                    request.ObserverSubscription.SiloAddress.ToParsableString());

                _subscriber = (GrainFactory.GetGrain(request.ObserverSubscription.SubscriberType, key) as IObserverSubscriber)!;
                logger.SuccessfullyPrepared(request.Partition);
                return Task.FromResult(Result.Success<JobStepPrepareStartError>());
            }

            logger.PreparingStoppedUnsubscribed(request.Partition);
            return Task.FromResult(Result.Failed(JobStepPrepareStartError.CouldNotPrepare));
        }
        catch (Exception e)
        {
            logger.FailedPreparing(e, nameof(HandleEventsForPartition));
            return Task.FromResult(Result.Failed(JobStepPrepareStartError.UnexpectedErrorPreparing));
        }
    }

    /// <inheritdoc/>
    protected override async Task<Catch<JobStepResult>> PerformStep(HandleEventsForPartitionArguments request, CancellationToken cancellationToken)
    {
        using var scope = logger.BeginObserverScope(request.ObserverKey, State.Id.JobId, State.Id.JobStepId);
        try
        {
            if (_subscriber is null || !request.ObserverSubscription.IsSubscribed)
            {
                logger.PerformingStoppedUnsubscribed(request.Partition);
                return JobStepResult.Failed(SubscriberDisconnected, "This should have been ensured in the Prepare operation");
            }
            if (cancellationToken.IsCancellationRequested)
            {
                logger.CancelledBeforeHandlingAnyEvents(request.Partition);
                return JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult());
            }
            var eventSequenceStorage = GetEventSequenceStorage(request.ObserverKey.EventStore, request.ObserverKey.Namespace, request.ObserverKey.EventSequenceId);
            using var events = await eventSequenceStorage.GetRange(
                request.StartEventSequenceNumber,
                request.EndEventSequenceNumber,
                _eventSourceId,
                request.EventTypes,
                cancellationToken);

            var subscriberContext = new ObserverSubscriberContext(request.ObserverSubscription.Arguments);

            var failed = false;
            var exceptionMessages = Enumerable.Empty<string>().ToArray();
            var exceptionStackTrace = string.Empty;

            var lastEventSequenceNumberAttempted = EventSequenceNumber.Unavailable;
            while (await events.MoveNext())
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    LogCancelled(lastEventSequenceNumberAttempted, request.Partition);
                    return JobStepResult.Failed(PerformJobStepError.CancelledWithPartialResult(CreateResult()));
                }
                var handledCount = EventCount.Zero;

                var handleEventsResult = await TryHandleEvents(request, events, subscriberContext);
                if (handleEventsResult.TryGetException(out var handleEventsException))
                {
                    failed = true;
                    exceptionMessages = handleEventsException.GetAllMessages().ToArray();
                    exceptionStackTrace = handleEventsException.StackTrace ?? string.Empty;
                    lastEventSequenceNumberAttempted = State.LastSuccessfullyHandledEventSequenceNumber.Next();
                }
                else
                {
                    var (eventObserverResult, handledEvents) = handleEventsResult.AsT0;
                    if (eventObserverResult.LastSuccessfulObservation.IsActualValue)
                    {
                        handledCount = events.Current.Count(_ => _.Metadata.SequenceNumber <= eventObserverResult.LastSuccessfulObservation);
                    }
                    switch (eventObserverResult.State)
                    {
                        case ObserverSubscriberState.Ok:
                            logger.SuccessfullyHandledEvents(request.Partition, handledCount, eventObserverResult.LastSuccessfulObservation);
                            lastEventSequenceNumberAttempted = EventSequenceNumber.Unavailable;
                            await PersistNewSuccessfullyHandledEventOrThrow(eventObserverResult.LastSuccessfulObservation);
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
                                await PersistNewSuccessfullyHandledEventOrThrow(eventObserverResult.LastSuccessfulObservation);
                            }

                            logger.FailedHandlingEvents(request.Partition, handledCount, lastEventSequenceNumberAttempted, State.LastSuccessfullyHandledEventSequenceNumber);
                            break;
                        case ObserverSubscriberState.Disconnected:
                            failed = true;
                            exceptionMessages = [SubscriberDisconnected];
                            lastEventSequenceNumberAttempted = State.LastSuccessfullyHandledEventSequenceNumber.Next();
                            logger.EventHandlerDisconnected(request.Partition, State.LastSuccessfullyHandledEventSequenceNumber);
                            break;
                    }
                }

                if (failed)
                {
                    var failedAt = lastEventSequenceNumberAttempted.IsActualValue ? lastEventSequenceNumberAttempted : request.StartEventSequenceNumber;
                    await _observer.PartitionFailed(_eventSourceId, failedAt, exceptionMessages, exceptionStackTrace);
                    return JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(CreateResult(), exceptionMessages, exceptionStackTrace));
                }
            }

            if (State.LastSuccessfullyHandledEventSequenceNumber == EventSequenceNumber.Unavailable)
            {
                logger.HandledNoneEvents(request.Partition);
            }
            else
            {
                logger.HandledAllEvents(request.Partition, State.LastSuccessfullyHandledEventSequenceNumber);
            }

            return JobStepResult.Succeeded(CreateResult());
        }
        catch (TaskCanceledException)
        {
            LogCancelled(State.LastSuccessfullyHandledEventSequenceNumber, request.Partition);
            return JobStepResult.Failed(PerformJobStepError.CancelledWithPartialResult(CreateResult()));
        }
        catch (Exception e)
        {
            logger.FailedPerforming(e, nameof(HandleEventsForPartition));
            if (!State.LastSuccessfullyHandledEventSequenceNumber.IsActualValue)
            {
                return e;
            }

            logger.FailedWithPartialSuccess(e, State.LastSuccessfullyHandledEventSequenceNumber);
            return JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(CreateResult(), e));
        }
    }

    static AppendedEvent[] SetObservationStateIfSpecified(HandleEventsForPartitionArguments request, IEventCursor events)
    {
        if (request.EventObservationState != EventObservationState.None)
        {
            return events.Current.Select(@event =>
                @event with
                {
                    Context = @event.Context with
                    {
                        ObservationState = request.EventObservationState
                    }
                }).ToArray();
        }

        return events.Current.ToArray();
    }

    async Task<Catch<(ObserverSubscriberResult Result, AppendedEvent[] HandledEvents), None>> TryHandleEvents(
        HandleEventsForPartitionArguments request,
        IEventCursor events,
        ObserverSubscriberContext subscriberContext)
    {
        try
        {
            var eventsToHandle = SetObservationStateIfSpecified(request, events);
            if (eventsToHandle.Length != 0)
            {
                var result = await _subscriber!.OnNext(eventsToHandle, subscriberContext);
                return (result, eventsToHandle);
            }

            logger.NoMoreEventsToHandle(request.Partition, request.StartEventSequenceNumber, request.EndEventSequenceNumber);
            return default(None);
        }
        catch (Exception ex)
        {
            logger.ErrorHandling(ex, request.Partition);
            return ex;
        }
    }
    async Task PersistNewSuccessfullyHandledEventOrThrow(EventSequenceNumber eventSequenceNumber)
    {
        State.LastSuccessfullyHandledEventSequenceNumber = eventSequenceNumber;
        var persistResult = await WriteStateAsync();
        if (persistResult.TryGetException(out var error))
        {
            logger.FailedToPersistSuccessfullyHandledEvent(error, eventSequenceNumber);
            persistResult.RethrowError();
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

    HandleEventsForPartitionResult CreateResult() => new(State.LastSuccessfullyHandledEventSequenceNumber);

    IEventSequenceStorage GetEventSequenceStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId) =>
        _eventSequenceStorage ??= storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(eventSequenceId);
}
