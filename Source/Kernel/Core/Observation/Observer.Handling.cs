// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
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
    public async Task ReportHandledEvents(Key partition, IEnumerable<AppendedEvent> handledEvents)
    {
        var countsPerEventType = CountByEventType(handledEvents);
        if (countsPerEventType.Count > 0)
        {
            await GetObserverHandledCountsStorage().Increment(_observerId, partition, countsPerEventType);
            State = WithIncrementedRunningTotals(State, countsPerEventType);
        }

        await WriteStateAsync();
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
        IReadOnlyDictionary<EventTypeId, EventCount> handledCountsPerEventType = ImmutableDictionary<EventTypeId, EventCount>.Empty;
        if (eventsToHandle.Length != 0)
        {
            // Record this partition as in-flight on the observer state and make it durable *before* the subscriber
            // is invoked. If the silo dies between this point and the post-handling state write, the marker survives
            // on the reloaded observer state and drives partition catch-up on the next activation.
            var inFlightPartitions = new HashSet<Key>(State.InFlightPartitions) { partition };
            State = State with { InFlightPartitions = inFlightPartitions };
            await WriteStateAsync();

            using (new WriteSuspension(this))
            {
                try
                {
                    var firstEvent = eventsToHandle[0];
                    tailEventSequenceNumber = firstEvent.Context.SequenceNumber;
                    var decryptedEvents = await DecryptEvents(eventsToHandle);

                    ObserverSubscriberResult result;
                    while (true)
                    {
                        var target = subscriberSelector.Select(_subscription, partition);
                        var key = new ObserverSubscriberKey(
                            _observerKey.ObserverId,
                            _observerKey.EventStore,
                            _observerKey.Namespace,
                            _observerKey.EventSequenceId,
                            partition,
                            target.SiloAddress.ToParsableString());

                        var subscriber = (GrainFactory.GetGrain(_subscription.SubscriberType, key) as IObserverSubscriber)!;
                        result = await subscriber.OnNext(partition, decryptedEvents, new(target.ConnectedClient ?? _subscription.Arguments));

                        // A disconnected result from one client instance only removes that instance —
                        // the batch is retried against the remaining instances. Only when the last
                        // instance is gone does the observer unsubscribe (below).
                        if (result.State == ObserverSubscriberState.Disconnected &&
                            target.ConnectedClient is not null &&
                            _subscription.Targets.Count > 1)
                        {
                            logger.ClientInstanceDisconnectedRetryingWithRemaining(target.ConnectedClient.ConnectionId, partition);
                            RemoveSubscriberTarget(target);
                            continue;
                        }

                        break;
                    }

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
                        handledCountsPerEventType = CountByEventType(handledEvents);
                        State = WithIncrementedRunningTotals(State, handledCountsPerEventType);
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
                // The in-flight marker has served its purpose now that the outcome is known, so clear it before the
                // outcome is persisted — whichever state write happens below carries the removal. A partition that
                // failed is recovered through FailedPartitions storage, so keeping its marker would only force a
                // redundant catch-up for events we already know about.
                var remainingInFlight = new HashSet<Key>(State.InFlightPartitions);
                remainingInFlight.Remove(partition);
                State = State with { InFlightPartitions = remainingInFlight };

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

                if (handledCountsPerEventType.Count > 0)
                {
                    await GetObserverHandledCountsStorage().Increment(_observerId, partition, handledCountsPerEventType);
                }
            }
            catch (Exception ex)
            {
                logger.ObserverFailedForUnknownReasonsAfterHandlingEvents(ex);
            }
        }
    }

    /// <summary>
    /// Counts the provided handled events, broken down by event type identifier.
    /// </summary>
    /// <param name="handledEvents">The events that were successfully handled.</param>
    /// <returns>The number of events per <see cref="EventTypeId"/>.</returns>
    static Dictionary<EventTypeId, EventCount> CountByEventType(IEnumerable<AppendedEvent> handledEvents)
    {
        var counts = new Dictionary<EventTypeId, EventCount>();
        foreach (var eventTypeId in handledEvents.Select(_ => _.Context.EventType.Id))
        {
            counts[eventTypeId] = counts.GetValueOrDefault(eventTypeId, EventCount.Zero) + 1UL;
        }

        return counts;
    }

    /// <summary>
    /// Returns a new <see cref="ObserverState"/> with the running handled-event totals incremented by the
    /// counts of a single handled batch. The per-partition breakdown lives in the dedicated
    /// <see cref="IObserverHandledCountsStorage"/> and is not part of <see cref="ObserverState"/>.
    /// </summary>
    /// <param name="state">The current <see cref="ObserverState"/> to update.</param>
    /// <param name="countsPerEventType">The number of events handled in the batch, broken down by <see cref="EventTypeId"/>.</param>
    /// <returns>A new <see cref="ObserverState"/> with <see cref="ObserverState.HandledEventCount"/> and <see cref="ObserverState.HandledEventCountPerEventType"/> incremented accordingly.</returns>
    static ObserverState WithIncrementedRunningTotals(
        ObserverState state,
        IReadOnlyDictionary<EventTypeId, EventCount> countsPerEventType)
    {
        if (countsPerEventType.Count == 0)
        {
            return state;
        }

        var perEventType = new Dictionary<EventTypeId, EventCount>(state.HandledEventCountPerEventType);
        var total = 0UL;
        foreach (var (eventTypeId, count) in countsPerEventType)
        {
            total += count.Value;
            perEventType[eventTypeId] = perEventType.GetValueOrDefault(eventTypeId, EventCount.Zero) + count.Value;
        }

        return state with
        {
            HandledEventCount = state.HandledEventCount + total,
            HandledEventCountPerEventType = perEventType
        };
    }

    /// <summary>
    /// Returns a new <see cref="ObserverState"/> with the given partition's contribution subtracted from the
    /// running handled-event totals. Used when a partition replay begins; the partition's counts come from the
    /// dedicated <see cref="IObserverHandledCountsStorage"/>.
    /// </summary>
    /// <param name="state">The current <see cref="ObserverState"/> to update.</param>
    /// <param name="partitionCounts">The partition's handled-event counts, broken down by <see cref="EventTypeId"/>.</param>
    /// <returns>A new <see cref="ObserverState"/> with the partition's counts subtracted from the aggregates.</returns>
    static ObserverState WithSubtractedPartitionHandledEventCounts(
        ObserverState state,
        IReadOnlyDictionary<EventTypeId, EventCount> partitionCounts)
    {
        if (partitionCounts.Count == 0)
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

        var newTotal = state.HandledEventCount.Value > totalForPartition
            ? state.HandledEventCount.Value - totalForPartition
            : 0UL;

        return state with
        {
            HandledEventCount = newTotal,
            HandledEventCountPerEventType = perEventType
        };
    }

    IObserverHandledCountsStorage GetObserverHandledCountsStorage() =>
        storage
            .GetEventStore(_observerKey.EventStore)
            .GetNamespace(_observerKey.Namespace)
            .ObserverHandledCounts;

    async Task<AppendedEvent[]> DecryptEvents(IEnumerable<AppendedEvent> events)
    {
        var eventsToDecrypt = events as AppendedEvent[] ?? events.ToArray();
        await EnsureEventTypeSchemasFor(eventsToDecrypt);
        return await eventCompliance.Release(eventsToDecrypt, _eventTypeSchemas);
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
}
