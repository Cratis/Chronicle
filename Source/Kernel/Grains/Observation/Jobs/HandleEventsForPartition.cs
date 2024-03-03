// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.EventSequences;
using Cratis.Jobs;
using Cratis.Kernel.Grains.Jobs;
using Cratis.Kernel.Storage;
using Cratis.Kernel.Storage.EventSequences;
using Cratis.Kernel.Storage.Jobs;
using Cratis.Observation;
using Orleans.Runtime;

namespace Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Represents a step in a replay job.
/// </summary>
public class HandleEventsForPartition : JobStep<HandleEventsForPartitionArguments, HandleEventsForPartitionResult, HandleEventsForPartitionState>, IHandleEventsForPartition
{
    const string SubscriberDisconnected = "Subscriber is disconnected";

    readonly IStorage _storage;
    IEventSequenceStorage? _eventSequenceStorage;
    IObserver? _observer;
    IObserverSubscriber? _subscriber;

    /// <summary>
    /// Initializes a new instance of the <see cref="HandleEventsForPartition"/> class.
    /// </summary>
    /// <param name="state"><see cref="IPersistentState{TState}"/> for managing state of the job step.</param>
    /// <param name="storage"><see cref="IStorage"/> for accessing storage for the cluster.</param>
    public HandleEventsForPartition(
        [PersistentState(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps)]
        IPersistentState<HandleEventsForPartitionState> state,
        IStorage storage) : base(state)
    {
        _storage = storage;
    }

    /// <inheritdoc/>
    public override Task Prepare(HandleEventsForPartitionArguments request)
    {
        var eventSourceId = (EventSourceId)(request.Partition.Value.ToString() ?? string.Empty);
        _observer = GrainFactory.GetGrain<IObserver>(request.ObserverId, request.ObserverKey);

        State.NextEventSequenceNumber = request.StartEventSequenceNumber;

        if (!request.ObserverSubscription.IsSubscribed)
        {
            return Task.CompletedTask;
        }

        var key = new ObserverSubscriberKey(
            request.ObserverKey.MicroserviceId,
            request.ObserverKey.TenantId,
            request.ObserverKey.EventSequenceId,
            eventSourceId,
            request.ObserverSubscription.SiloAddress.ToParsableString());

        _subscriber = (GrainFactory.GetGrain(request.ObserverSubscription.SubscriberType, request.ObserverSubscription.ObserverId, key) as IObserverSubscriber)!;

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override async Task<JobStepResult> PerformStep(HandleEventsForPartitionArguments request, CancellationToken cancellationToken)
    {
        if (_subscriber == null || !request.ObserverSubscription.IsSubscribed)
        {
            return JobStepResult.Failed(SubscriberDisconnected);
        }

        var eventSourceId = (EventSourceId)(request.Partition.Value.ToString() ?? string.Empty);
        var eventSequenceStorage = GetEventSequenceStorage(request.ObserverKey.MicroserviceId, request.ObserverKey.TenantId, request.ObserverKey.EventSequenceId);
        using var events = await eventSequenceStorage.GetFromSequenceNumber(
            request.StartEventSequenceNumber,
            eventSourceId,
            request.EventTypes);

        var subscriberContext = new ObserverSubscriberContext(request.ObserverSubscription.Arguments);

        var failed = false;
        var exceptionMessages = Enumerable.Empty<string>();
        var exceptionStackTrace = string.Empty;
        var tailEventSequenceNumber = EventSequenceNumber.Unavailable;
        var handledCount = EventCount.Zero;

        while (await events.MoveNext())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                if (!events.Current.Any())
                {
                    break;
                }

                tailEventSequenceNumber = events.Current.First().Metadata.SequenceNumber;

                var eventsToHandle = events.Current;

                eventsToHandle = SetObservationStateIfSpecified(request, events, eventsToHandle);
                var result = await _subscriber!.OnNext(eventsToHandle, subscriberContext);
                if (result.LastSuccessfulObservation != EventSequenceNumber.Unavailable)
                {
                    handledCount = events.Current.Count(_ => _.Metadata.SequenceNumber <= result.LastSuccessfulObservation);
                }
                else
                {
                    handledCount = EventCount.Zero;
                }
                switch (result.State)
                {
                    case ObserverSubscriberState.Failed:
                        failed = true;
                        exceptionMessages = result.ExceptionMessages;
                        exceptionStackTrace = result.ExceptionStackTrace;
                        if (result.LastSuccessfulObservation != EventSequenceNumber.Unavailable)
                        {
                            tailEventSequenceNumber = result.LastSuccessfulObservation;
                        }
                        break;
                    case ObserverSubscriberState.Disconnected:
                        failed = true;
                        exceptionMessages = new[] { SubscriberDisconnected };
                        break;
                }
            }
            catch (Exception ex)
            {
                failed = true;
                exceptionMessages = ex.GetAllMessages().ToArray();
                exceptionStackTrace = ex.StackTrace ?? string.Empty;
            }

            if (failed)
            {
                await _observer!.PartitionFailed(eventSourceId, tailEventSequenceNumber, exceptionMessages, exceptionStackTrace);
                return new JobStepResult(JobStepStatus.Failed, exceptionMessages, exceptionStackTrace);
            }
        }

        return JobStepResult.Succeeded(new HandleEventsForPartitionResult(handledCount));
    }

    IEnumerable<AppendedEvent> SetObservationStateIfSpecified(HandleEventsForPartitionArguments request, IEventCursor events, IEnumerable<AppendedEvent> eventsToHandle)
    {
        if (request.EventObservationState != EventObservationState.None)
        {
            eventsToHandle = events.Current.Select(@event =>
                @event with
                {
                    Context = @event.Context with
                    {
                        ObservationState = request.EventObservationState
                    }
                }).ToArray();
        }

        return eventsToHandle;
    }

    IEventSequenceStorage GetEventSequenceStorage(MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId) => _eventSequenceStorage ??= _storage.GetEventStore((string)microserviceId).GetNamespace(tenantId).GetEventSequence(eventSequenceId);
}
