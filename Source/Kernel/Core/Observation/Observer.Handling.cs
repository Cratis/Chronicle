// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    ObserverSubscription? _eventTypeSubscription;
    FrozenSet<EventTypeId> _subscribedEventTypeIds = [];

    bool CannotHandleEvents => IsRetired || _removed || !_observerExists;

    /// <inheritdoc/>
    public Task SetHandledStats(EventSequenceNumber lastHandledEventSequenceNumber)
    {
        if (CannotHandleEvents) return Task.CompletedTask;
        State = State with
        {
            LastHandledEventSequenceNumber = lastHandledEventSequenceNumber
        };
        RecordedHandlingState();

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
            NoteHeldBackEvents(partition, events);
            return;
        }

        using var span = activitySource.Handle();
        span?.Activity?.Tag(_observerKey);
        span?.Activity?.Tag(Definition.Type);
        if (span?.Activity is { IsAllDataRequested: true } activity && events.TryGetNonEnumeratedCount(out var eventCount))
        {
            activity.TagEventCount(eventCount);
        }

        try
        {
            await HandleEventsForPartition(partition, events, span?.Activity);
        }
        catch (Exception ex)
        {
            span?.Activity?.RecordFailure(ex);
            throw;
        }
        finally
        {
            EvaluateSuspectedSkips();
        }
    }

    bool HasSubscribedEvent(IEnumerable<AppendedEvent> events)
    {
        if (!ReferenceEquals(_eventTypeSubscription, _subscription))
        {
            _subscribedEventTypeIds = _subscription.EventTypes.Select(static eventType => eventType.Id).ToFrozenSet();
            _eventTypeSubscription = _subscription;
        }
        foreach (var @event in events)
        {
            if (_subscribedEventTypeIds.Contains(@event.Context.EventType.Id)) return true;
        }
        return false;
    }

    async Task<AppendedEvent[]> DecryptEvents(IEnumerable<AppendedEvent> events)
    {
        if (_subscription.GenerationDelivery == EventGenerationDelivery.Pinned)
        {
            return await (eventGenerationRelease ?? throw new EventGenerationDeliveryNotSupported()).Release(_observerKey.EventStore, _subscription.EventTypes, _eventTypeSchemas, events);
        }

        var eventsToDecrypt = events as AppendedEvent[] ?? events.ToArray();
        await storage.GetEventStore(_observerKey.EventStore).EventTypes.EnsureSchemasFor(_eventTypeSchemas, eventsToDecrypt);
        return await eventCompliance.Release(eventsToDecrypt, _eventTypeSchemas);
    }

    bool ShouldHandleEvent(Key partition)
    {
        if (CannotHandleEvents) return false;
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

        return IsActiveForDelivery() && PartitionIsAvailableForDelivery(partition);
    }

    bool IsActiveForDelivery()
    {
        if (IsQuarantined || State.RunningState != ObserverRunningState.Active)
        {
            logger.ObserverIsNotActive();
            return false;
        }

        if (_isPreparingCatchup)
        {
            logger.ObserverIsPreparingCatchup();
            return false;
        }

        return true;
    }

    bool PartitionIsAvailableForDelivery(Key partition)
    {
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

        // The walk reads every partition to the end of the log, including one whose first event arrives after it registered
        // its partitions; delivering that event live as well would hand it to the subscriber twice.
        if (_walkingEveryPartition)
        {
            logger.CatchUpWalkCoversNewEvents(partition);
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
        RecordedHandlingState();
    }
}
