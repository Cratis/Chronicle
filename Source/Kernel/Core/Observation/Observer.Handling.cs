// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <inheritdoc/>
    public Task SetHandledStats(EventSequenceNumber lastHandledEventSequenceNumber)
    {
        State = State with
        {
            LastHandledEventSequenceNumber = lastHandledEventSequenceNumber
        };

        return WriteStateAsync();
    }

    /// <inheritdoc/>
    public Task ReportHandledEvents(Key partition, IEnumerable<AppendedEvent> handledEvents)
    {
        State = WithIncrementedHandledEventCounts(State, partition, handledEvents);
        return WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task Handle(Key partition, IEnumerable<AppendedEvent> events)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);

        if (!events.Any())
        {
            return;
        }

        if (!ShouldHandleEvent(partition))
        {
            return;
        }

        var observedTailEventSequenceNumber = events.Last().Context.SequenceNumber;

        if (!events.Any(_ => _subscription.EventTypes.Any(et => et.Id == _.Context.EventType.Id)))
        {
            State = State with
            {
                NextEventSequenceNumber = observedTailEventSequenceNumber.Next(),
                TailEventSequenceNumber = observedTailEventSequenceNumber
            };
            await WriteStateAsync();
            return;
        }

        if (_subscription.Filters is { } filters)
        {
            if (filters.EventSourceType is { } eventSourceType &&
                eventSourceType != EventSourceType.Unspecified &&
                !events.Any(_ => _.Context.EventSourceType == eventSourceType))
            {
                State = State with
                {
                    NextEventSequenceNumber = observedTailEventSequenceNumber.Next(),
                    TailEventSequenceNumber = observedTailEventSequenceNumber
                };
                await WriteStateAsync();
                return;
            }

            if (filters.EventStreamType is { } eventStreamType &&
                !eventStreamType.IsAll &&
                !events.Any(_ => _.Context.EventStreamType == eventStreamType))
            {
                State = State with
                {
                    NextEventSequenceNumber = observedTailEventSequenceNumber.Next(),
                    TailEventSequenceNumber = observedTailEventSequenceNumber
                };
                await WriteStateAsync();
                return;
            }

            if (filters.Tags.Any() &&
                !events.Any(_ => _.Context.Tags.Any(t => filters.Tags.Contains(t.Value))))
            {
                State = State with
                {
                    NextEventSequenceNumber = observedTailEventSequenceNumber.Next(),
                    TailEventSequenceNumber = observedTailEventSequenceNumber
                };
                await WriteStateAsync();
                return;
            }
        }

        var failed = false;
        var exceptionMessages = Enumerable.Empty<string>();
        var exceptionStackTrace = string.Empty;
        var tailEventSequenceNumber = State.NextEventSequenceNumber;

        var eventsToHandle = events.Where(_ => _.Context.SequenceNumber >= tailEventSequenceNumber).ToArray();
        var numEventsSuccessfullyHandled = EventCount.Zero;
        var stateChanged = false;
        if (eventsToHandle.Length != 0)
        {
            using (new WriteSuspension(this))
            {
                try
                {
                    var key = new ObserverSubscriberKey(
                        _observerKey.ObserverId,
                        _observerKey.EventStore,
                        _observerKey.Namespace,
                        _observerKey.EventSequenceId,
                        partition,
                        _subscription.SiloAddress.ToParsableString());

                    var firstEvent = eventsToHandle[0];

                    var subscriber = (GrainFactory.GetGrain(_subscription.SubscriberType, key) as IObserverSubscriber)!;
                    tailEventSequenceNumber = firstEvent.Context.SequenceNumber;
                    var decryptedEvents = await DecryptEvents(eventsToHandle);
                    var result = await subscriber.OnNext(partition, decryptedEvents, new(_subscription.Arguments));
                    numEventsSuccessfullyHandled = result.HandledAnyEvents
                        ? eventsToHandle.Count(_ => _.Context.SequenceNumber <= result.LastSuccessfulObservation)
                        : EventCount.Zero;

                    if (result.State == ObserverSubscriberState.Failed)
                    {
                        failed = true;
                        exceptionMessages = result.ExceptionMessages;
                        exceptionStackTrace = result.ExceptionStackTrace;
                        tailEventSequenceNumber = result.HandledAnyEvents
                            ? result.LastSuccessfulObservation
                            : firstEvent.Context.SequenceNumber;
                    }
                    else if (result.State == ObserverSubscriberState.Disconnected)
                    {
                        await Unsubscribe();
                        stateChanged = true;
                    }

                    if (numEventsSuccessfullyHandled > 0)
                    {
                        stateChanged = true;
                        State = State with
                        {
                            NextEventSequenceNumber = result.LastSuccessfulObservation.Next(),
                            TailEventSequenceNumber = observedTailEventSequenceNumber
                        };
                        var previousLastHandled = State.LastHandledEventSequenceNumber;
                        var shouldSetLastHandled =
                            previousLastHandled == EventSequenceNumber.Unavailable ||
                            previousLastHandled < result.LastSuccessfulObservation;
                        State = State with
                        {
                            LastHandledEventSequenceNumber = shouldSetLastHandled
                                ? result.LastSuccessfulObservation
                                : previousLastHandled,
                        };

                        var handledEvents = decryptedEvents.Where(_ => _.Context.SequenceNumber <= result.LastSuccessfulObservation);
                        State = WithIncrementedHandledEventCounts(State, partition, handledEvents);
                    }
                }
                catch (Exception ex)
                {
                    failed = true;
                    exceptionMessages = ex.GetAllMessages().ToArray();
                    exceptionStackTrace = ex.StackTrace ?? string.Empty;
                }
            }

            try
            {
                if (failed)
                {
                    await PartitionFailed(partition, tailEventSequenceNumber, exceptionMessages, exceptionStackTrace);
                }
                else
                {
                    _metrics?.SuccessfulObservation();
                }

                if (stateChanged)
                {
                    await WriteStateAsync();
                }
            }
            catch (Exception ex)
            {
                logger.ObserverFailedForUnknownReasonsAfterHandlingEvents(ex);
            }
        }
    }

    /// <summary>
    /// Returns a new <see cref="Storage.Observation.ObserverState"/> with handled event counts incremented
    /// for the given partition and the provided successfully handled events.
    /// </summary>
    /// <param name="state">The current <see cref="Storage.Observation.ObserverState"/> to update.</param>
    /// <param name="partition">The <see cref="Key"/> identifying the partition whose counts to increment.</param>
    /// <param name="handledEvents">The events that were successfully handled.</param>
    static Storage.Observation.ObserverState WithIncrementedHandledEventCounts(
        Storage.Observation.ObserverState state,
        Key partition,
        IEnumerable<AppendedEvent> handledEvents)
    {
        var perEventType = new Dictionary<EventTypeId, EventCount>(state.HandledEventCountPerEventType);
        var perPartition = new Dictionary<Key, IReadOnlyDictionary<EventTypeId, EventCount>>(state.HandledEventCountPerPartition);
        var partitionCounts = perPartition.TryGetValue(partition, out var existing)
            ? new Dictionary<EventTypeId, EventCount>(existing)
            : [];

        var count = 0UL;
        foreach (var eventTypeId in handledEvents.Select(_ => _.Context.EventType.Id))
        {
            count++;
            perEventType[eventTypeId] = perEventType.GetValueOrDefault(eventTypeId, EventCount.Zero) + 1UL;
            partitionCounts[eventTypeId] = partitionCounts.GetValueOrDefault(eventTypeId, EventCount.Zero) + 1UL;
        }

        if (count == 0)
        {
            return state;
        }

        perPartition[partition] = partitionCounts;
        return state with
        {
            HandledEventCount = state.HandledEventCount + count,
            HandledEventCountPerEventType = perEventType,
            HandledEventCountPerPartition = perPartition
        };
    }

    /// <summary>
    /// Returns a new <see cref="Storage.Observation.ObserverState"/> with the given partition's contribution
    /// subtracted from all handled event counts. Used when a partition replay begins.
    /// </summary>
    /// <param name="state">The current <see cref="Storage.Observation.ObserverState"/> to update.</param>
    /// <param name="partition">The <see cref="Key"/> identifying the partition whose counts to subtract.</param>
    static Storage.Observation.ObserverState WithSubtractedPartitionHandledEventCounts(
        Storage.Observation.ObserverState state,
        Key partition)
    {
        if (!state.HandledEventCountPerPartition.TryGetValue(partition, out var partitionCounts))
        {
            return state;
        }

        var perEventType = new Dictionary<EventTypeId, EventCount>(state.HandledEventCountPerEventType);
        var totalForPartition = 0UL;
        foreach (var (eventTypeId, count) in partitionCounts)
        {
            totalForPartition += count.Value;
            if (perEventType.TryGetValue(eventTypeId, out var existing))
            {
                var newCount = existing.Value > count.Value ? existing.Value - count.Value : 0UL;
                if (newCount == 0)
                {
                    perEventType.Remove(eventTypeId);
                }
                else
                {
                    perEventType[eventTypeId] = newCount;
                }
            }
        }

        var perPartition = new Dictionary<Key, IReadOnlyDictionary<EventTypeId, EventCount>>(state.HandledEventCountPerPartition);
        perPartition.Remove(partition);

        var newTotal = state.HandledEventCount.Value > totalForPartition
            ? state.HandledEventCount.Value - totalForPartition
            : 0UL;

        return state with
        {
            HandledEventCount = newTotal,
            HandledEventCountPerEventType = perEventType,
            HandledEventCountPerPartition = perPartition
        };
    }

    Task<AppendedEvent[]> DecryptEvents(IEnumerable<AppendedEvent> events) =>
        EventComplianceHelper.DecryptEvents(complianceManager, expandoObjectConverter, events, _eventTypeSchemas);

    bool ShouldHandleEvent(Key partition)
    {
        if (!_subscription.IsSubscribed)
        {
            logger.ObserverIsNotSubscribed();
            return false;
        }

        if (Failures.IsFailed(partition))
        {
            logger.PartitionIsFailed(partition);
            return false;
        }

        if (State.RunningState != ObserverRunningState.Active)
        {
            logger.ObserverIsNotActive();
            return false;
        }

        if (_isPreparingCatchup)
        {
            logger.ObserverIsPreparingCatchup();
            return false;
        }

        if (State.ReplayingPartitions.Contains(partition))
        {
            logger.PartitionReplayingCannotHandleNewEvents(partition);
            return false;
        }

        if (State.CatchingUpPartitions.Contains(partition))
        {
            logger.PartitionCatchingUpCannotHandleNewEvents(partition);
            return false;
        }

        return true;
    }

    void HandleNewLastHandledEvent(EventSequenceNumber lastHandledEvent)
    {
        if (!lastHandledEvent.IsActualValue)
        {
            logger.LastHandledEventIsNotActualValue();
            return;
        }

        var newLastHandledEvent = State.LastHandledEventSequenceNumber == EventSequenceNumber.Unavailable ||
                                  State.LastHandledEventSequenceNumber < lastHandledEvent ? lastHandledEvent : State.LastHandledEventSequenceNumber;
        var nextEventSequenceNumber = State.NextEventSequenceNumber <= lastHandledEvent ? lastHandledEvent.Next() : State.NextEventSequenceNumber;
        State = State with
        {
            LastHandledEventSequenceNumber = newLastHandledEvent,
            NextEventSequenceNumber = nextEventSequenceNumber
        };
    }
}
