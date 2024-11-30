// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Jobs;
using Microsoft.Extensions.Logging;

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
    ILogger<HandleEventsForPartition> logger) : JobStep<HandleEventsForPartitionArguments, HandleEventsForPartitionResult, HandleEventsForPartitionState>(state), IHandleEventsForPartition
{
    const string SubscriberDisconnected = "Subscriber is disconnected";

    IEventSequenceStorage? _eventSequenceStorage;
    IObserver _observer = null!;
    EventSourceId _eventSourceId = EventSourceId.Unspecified;
    IObserverSubscriber? _subscriber;

    /// <inheritdoc/>
    public override Task Prepare(HandleEventsForPartitionArguments request)
    {
        using var scope = logger.BeginObserverScope(request.ObserverKey, State.Id.JobId, State.Id.JobStepId);
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
            return Task.CompletedTask;
        }

        logger.PreparingStoppedUnsubscribed(request.Partition);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override async Task<JobStepResult> PerformStep(HandleEventsForPartitionArguments request, CancellationToken cancellationToken)
    {
        using var scope = logger.BeginObserverScope(request.ObserverKey, State.Id.JobId, State.Id.JobStepId);
        if (_subscriber is null || !request.ObserverSubscription.IsSubscribed)
        {
            logger.PerformingStoppedUnsubscribed(request.Partition);
            return JobStepResult.Failed(SubscriberDisconnected);
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
        var exceptionMessages = Enumerable.Empty<string>();
        var exceptionStackTrace = string.Empty;

        var lastEventSequenceNumberAttempted = EventSequenceNumber.Unavailable;
        while (await events.MoveNext())
        {
            var handledCount = EventCount.Zero;
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var eventsToHandle = SetObservationStateIfSpecified(request, events);
                if (eventsToHandle.Length == 0)
                {
                    logger.NoMoreEventsToHandle(request.Partition, request.StartEventSequenceNumber, request.EndEventSequenceNumber);
                    break;
                }

                lastEventSequenceNumberAttempted = eventsToHandle[0].Metadata.SequenceNumber;
                var result = await _subscriber!.OnNext(eventsToHandle, subscriberContext);
                if (result.LastSuccessfulObservation != EventSequenceNumber.Unavailable)
                {
                    handledCount = events.Current.Count(_ => _.Metadata.SequenceNumber <= result.LastSuccessfulObservation);
                }

                switch (result.State)
                {
                    case ObserverSubscriberState.Ok:
                        logger.SuccessfullyHandledEvents(request.Partition, handledCount, result.LastSuccessfulObservation);
                        lastEventSequenceNumberAttempted = EventSequenceNumber.Unavailable;
                        State.LastSuccessfullyHandledEventSequenceNumber = result.LastSuccessfulObservation;
                        await WriteStateAsync();
                        break;
                    case ObserverSubscriberState.Failed:
                        failed = true;
                        exceptionMessages = result.ExceptionMessages;
                        exceptionStackTrace = result.ExceptionStackTrace;
                        lastEventSequenceNumberAttempted = result.HandledAnyEvents
                            ? eventsToHandle.First(e => e.Metadata.SequenceNumber > result.LastSuccessfulObservation).Metadata.SequenceNumber
                            : lastEventSequenceNumberAttempted;
                        if (result.HandledAnyEvents)
                        {
                            State.LastSuccessfullyHandledEventSequenceNumber = result.LastSuccessfulObservation;
                            await WriteStateAsync();
                        }

                        logger.FailedHandlingEvents(request.Partition, handledCount, lastEventSequenceNumberAttempted, State.LastSuccessfullyHandledEventSequenceNumber);
                        break;
                    case ObserverSubscriberState.Disconnected:
                        failed = true;
                        exceptionMessages = [SubscriberDisconnected];
                        logger.EventHandlerDisconnected(request.Partition, State.LastSuccessfullyHandledEventSequenceNumber);
                        break;
                }
            }
            catch (Exception ex)
            {
                failed = true;
                exceptionMessages = ex.GetAllMessages().ToArray();
                exceptionStackTrace = ex.StackTrace ?? string.Empty;
                logger.ErrorHandling(ex, request.Partition);
            }

            if (failed)
            {
                if (lastEventSequenceNumberAttempted == EventSequenceNumber.Unavailable &&
                    State.LastSuccessfullyHandledEventSequenceNumber != EventSequenceNumber.Unavailable)
                {
                    lastEventSequenceNumberAttempted = State.LastSuccessfullyHandledEventSequenceNumber.Next();
                }

                await _observer!.PartitionFailed(_eventSourceId, lastEventSequenceNumberAttempted, exceptionMessages, exceptionStackTrace);
                return new(JobStepStatus.Failed, exceptionMessages, exceptionStackTrace);
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

        return JobStepResult.Succeeded(new HandleEventsForPartitionResult(State.LastSuccessfullyHandledEventSequenceNumber));
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

    IEventSequenceStorage GetEventSequenceStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId) =>
        _eventSequenceStorage ??= storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(eventSequenceId);
}
