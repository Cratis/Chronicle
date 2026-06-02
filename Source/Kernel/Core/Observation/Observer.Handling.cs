// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Storage.Observation;

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
        using var span = activitySource.Handle();
        span?.Activity?.Tag(_observerKey);
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

        // Apply the structural event-type subscription and the dynamic ObserverFilters per-event,
        // not at batch granularity. The previous batch-level checks ("skip if NO event in the
        // batch passes the filter") let non-matching events leak through whenever the same batch
        // also carried at least one matching event — a reactor with an EventSourceType filter
        // would then have its handler invoked for every event in such a mixed batch. With the
        // matching set computed up front, the subscriber sees only the events the observer
        // actually subscribed to and the cursor still advances cleanly past everything that was
        // filtered out.
        var matchingEvents = events.Where(EventMatchesSubscription).ToArray();
        if (matchingEvents.Length == 0)
        {
            State = State with
            {
                NextEventSequenceNumber = observedTailEventSequenceNumber.Next(),
                TailEventSequenceNumber = observedTailEventSequenceNumber
            };
            await WriteStateAsync();
            return;
        }

        var failed = false;
        var exceptionMessages = Enumerable.Empty<string>();
        var exceptionStackTrace = string.Empty;
        var tailEventSequenceNumber = State.NextEventSequenceNumber;

        var eventsToHandle = matchingEvents.Where(_ => _.Context.SequenceNumber >= tailEventSequenceNumber).ToArray();
        var numEventsSuccessfullyHandled = EventCount.Zero;
        var stateChanged = false;
        if (eventsToHandle.Length != 0)
        {
            // Record the highest sequence number we're about to attempt for this partition. If the silo
            // dies between this point and the post-handling state write, the entry survives to drive
            // partition catch-up on the next activation. Recording the tail of the batch is enough: the
            // in-flight entry is conceptually a marker that work *may have started* for this partition.
            var inFlightStorage = await GetInFlightEventsStorage();
            var inFlightTail = eventsToHandle[^1].Context.SequenceNumber;
            await inFlightStorage.Add(_observerId, partition, inFlightTail);

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

                        // When every matching event in the batch was successfully handled we can
                        // advance past the batch tail rather than past the last matching event —
                        // any non-matching events between LastSuccessfulObservation and the batch
                        // tail were already filtered, re-fetching them would just cost a round
                        // trip to filter them again. Partial success keeps the conservative
                        // LastSuccessfulObservation.Next() so the next attempt retries from the
                        // first unhandled matching event.
                        var allMatchingHandled = numEventsSuccessfullyHandled.Value == (ulong)eventsToHandle.Length;
                        var nextSequence = allMatchingHandled
                            ? observedTailEventSequenceNumber.Next()
                            : result.LastSuccessfulObservation.Next();

                        State = State with
                        {
                            NextEventSequenceNumber = nextSequence,
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

                // Clear in-flight markers for everything we processed (successfully or otherwise). Failures
                // are tracked through FailedPartitions storage; keeping the in-flight marker would force a
                // double recovery for events we already know about.
                var clearUpTo = numEventsSuccessfullyHandled > 0
                    ? eventsToHandle.Take((int)numEventsSuccessfullyHandled.Value).Last().Context.SequenceNumber
                    : tailEventSequenceNumber;
                await inFlightStorage.RemoveUpTo(_observerId, partition, clearUpTo);
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
    /// <returns>A new <see cref="Storage.Observation.ObserverState"/> with <see cref="Storage.Observation.ObserverState.HandledEventCount"/>, <see cref="Storage.Observation.ObserverState.HandledEventCountPerEventType"/>, and <see cref="Storage.Observation.ObserverState.HandledEventCountPerPartition"/> incremented accordingly.</returns>
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
    /// subtracted from all handled event counts, and the partition removed from <see cref="Storage.Observation.ObserverState.HandledEventCountPerPartition"/>.
    /// Used when a partition replay begins.
    /// </summary>
    /// <param name="state">The current <see cref="Storage.Observation.ObserverState"/> to update.</param>
    /// <param name="partition">The <see cref="Key"/> identifying the partition whose counts to subtract.</param>
    /// <returns>A new <see cref="Storage.Observation.ObserverState"/> with the partition's counts removed and aggregates adjusted.</returns>
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

    Task<IInFlightEventsStorage> GetInFlightEventsStorage() =>
        Task.FromResult(storage
            .GetEventStore(_observerKey.EventStore)
            .GetNamespace(_observerKey.Namespace)
            .InFlightEvents);

    async Task<AppendedEvent[]> DecryptEvents(IEnumerable<AppendedEvent> events)
    {
        var eventsToDecrypt = events as AppendedEvent[] ?? events.ToArray();
        await EnsureEventTypeSchemasFor(eventsToDecrypt);
        return await eventComplianceHelper.DecryptEvents(eventsToDecrypt, _eventTypeSchemas);
    }

    async Task EnsureEventTypeSchemasFor(IEnumerable<AppendedEvent> events)
    {
        var missingEventTypes = events
            .Select(_ => _.Context.EventType)
            .Distinct()
            .Where(_ => !_eventTypeSchemas.ContainsKey(_))
            .ToArray();

        if (missingEventTypes.Length == 0)
        {
            return;
        }

        var schemas = await storage.GetEventStore(_observerKey.EventStore).EventTypes.GetFor(missingEventTypes);
        foreach (var schema in schemas)
        {
            _eventTypeSchemas[schema.Type] = schema;
        }
    }

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

    /// <summary>
    /// Returns true when the event matches the structural event-type subscription and all dynamic
    /// <see cref="ObserverFilters"/> the subscription declared. Used to drop non-matching events
    /// from a batch before dispatching to the subscriber so partial-match batches no longer leak
    /// the non-matching tail to the handler.
    /// </summary>
    /// <param name="event">The <see cref="AppendedEvent"/> to evaluate.</param>
    /// <returns>True when the event should reach the subscriber; false when it should be skipped.</returns>
    bool EventMatchesSubscription(AppendedEvent @event)
    {
        if (!_subscription.EventTypes.Any(et => et.Id == @event.Context.EventType.Id))
        {
            return false;
        }

        if (_subscription.Filters is not { } filters)
        {
            return true;
        }

        if (filters.EventSourceType is { } eventSourceType
            && eventSourceType != EventSourceType.Unspecified
            && @event.Context.EventSourceType != eventSourceType)
        {
            return false;
        }

        if (filters.EventStreamType is { } eventStreamType
            && !eventStreamType.IsAll
            && @event.Context.EventStreamType != eventStreamType)
        {
            return false;
        }

        if (filters.Tags.Any()
            && !@event.Context.Tags.Any(t => filters.Tags.Contains(t.Value)))
        {
            return false;
        }

        return true;
    }
}
