// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    static string FailureKindCode(FailureKind kind) => kind switch
    {
        FailureKind.Handling => "handling",
        FailureKind.Timeout => "timeout",
        FailureKind.Disconnected => "disconnected",
        FailureKind.WaitingForHandler => "waiting_for_handler",
        _ => "unknown"
    };

    async Task HandleEventsForPartition(Key partition, IEnumerable<AppendedEvent> events, Activity? activity)
    {
        var observedTail = events.Last().Context.SequenceNumber;
        if (ShouldSkipPartition(events))
        {
            AdvancePastUnsubscribedTail(observedTail);
            await WriteProgressStateDebounced();
            return;
        }

        var tail = _pageFloor ?? State.NextEventSequenceNumber;
        var outcome = new HandlingOutcome(tail);
        var eventsToHandle = events.Where(_ => _.Context.SequenceNumber >= tail).ToArray();
        NoteDiscardedEvents(partition, events, tail);
        var configuration = await configurationProvider.GetFor(_observerKeyText);
        if (ExceedsLiveBudget(configuration, eventsToHandle.Length) && await HandOverLargeLiveBatchToCatchUp(partition, eventsToHandle)) return;
        if (!await CanDispatchDelivery(partition, eventsToHandle)) return;

        // Only a delivery that reaches the subscriber can race a catch-up; a hand-over holds its partition instead.
        using var delivery = BeginLiveDelivery();
        MarkPartitionInFlight(partition);
        await WriteStateAsync();
        if (!_observerExists) return;

        using (new WriteSuspension(this))
        {
            try
            {
                var firstEvent = eventsToHandle[0];
                outcome.Tail = firstEvent.Context.SequenceNumber;
                var decryptedEvents = await DecryptEvents(eventsToHandle);
                var subscriberTimeout = GetLiveSubscriberTimeout(configuration.GetSubscriberTimeout());
                var hold = await GetClientDeliveryHold();
                ObserverSubscriberResult result;
                while (true)
                {
                    var target = subscriberSelector.Select(_subscription, partition);
                    var key = SubscriberKeyFor(partition, target);
                    if (TryTakePrefetchedDelivery(partition, target, out var prefetched))
                    {
                        // The page this partition belongs to has already been delivered; this is its settlement.
                        outcome.DeliveryFence = prefetched.Fence;
                        if (prefetched.Failure is not null) throw prefetched.Failure;
                        result = prefetched.Result!;
                        break;
                    }

                    var subscriber = (GrainFactory.GetGrain(_subscription.SubscriberType, key) as IObserverSubscriber)!;
                    var context = new ObserverSubscriberContext(target.ConnectedClient ?? _subscription.Arguments) { Origin = ObservationOrigin.Live }.WithDeadlines(subscriberTimeout, hold, _clock);
                    outcome.DeliveryFence = AbandonedDeliveryDrain.Capture(_subscription, partition, target, context);
                    result = await subscriber.OnNextWithin(subscriberTimeout, partition, decryptedEvents, context);
                    if (!CanRetryDisconnectedClient(result, target)) break;

                    // Retry against the remaining instances; only the last disconnected instance unsubscribes.
                    logger.ClientInstanceDisconnectedRetryingWithRemaining(target.ConnectedClient!.ConnectionId, partition);
                    RemoveSubscriberTarget(target);
                }

                var numberHandled = result.HandledAnyEvents
                    ? eventsToHandle.Count(_ => _.Context.SequenceNumber <= result.LastSuccessfulObservation)
                    : EventCount.Zero;
                outcome.RecordResult(result, firstEvent.Context.SequenceNumber);
                if (result.State is ObserverSubscriberState.Deferred or ObserverSubscriberState.Disconnected)
                {
                    await ApplySubscriberState(partition, result);
                    outcome.StateChanged = true;
                }

                if (numberHandled > 0)
                {
                    outcome.StateChanged = true;
                    RecordHandledProgress(result, decryptedEvents, observedTail, ref outcome);
                }
            }
            catch (Exception ex)
            {
                outcome.RecordException(ex);
            }
        }

        try
        {
            ClearPartitionInFlight(partition);
            await RecordHandlingOutcome(partition, outcome, activity);
            if (outcome.StateChanged) await WriteStateAsync();
            if (_observerExists) await CoalesceLiveHandledCounts(partition, outcome.HandledCounts);
        }
        catch (Exception ex)
        {
            activity?.RecordFailure(ex);
            logger.ObserverFailedForUnknownReasonsAfterHandlingEvents(ex);
        }
    }

    // A disconnected batch is not advanced, so the next subscription catches up from the same position.
    Task ApplySubscriberState(Key partition, ObserverSubscriberResult result) =>
        result.State == ObserverSubscriberState.Deferred
            ? PartitionDeferred(partition, result.Deferral!)
            : PromoteSupersededRegistrationOrUnsubscribe();

    bool ShouldSkipPartition(IEnumerable<AppendedEvent> events)
    {
        // Wildcard subscriptions have no fixed event type list: only their filters can exclude the batch.
        if (!State.SubscribesToAllEvents && !HasSubscribedEvent(events)) return true;
        return ExcludedByFilters([.. events]);
    }

    void AdvancePastUnsubscribedTail(EventSequenceNumber observedTail)
    {
        State = State with
        {
            NextEventSequenceNumber = CursorAfter(observedTail.Next()),
            TailEventSequenceNumber = TailAfter(observedTail)
        };
        RecordedHandlingState();
    }

    void MarkPartitionInFlight(Key partition)
    {
        // Make the marker durable before invoking the subscriber, so a silo crash drives catch-up on activation.
        var inFlightPartitions = new HashSet<Key>(State.InFlightPartitions) { partition };
        State = State with { InFlightPartitions = inFlightPartitions };
        RecordedHandlingState();
    }

    void ClearPartitionInFlight(Key partition)
    {
        // Failure storage recovers a failed partition; retaining its marker would force redundant catch-up.
        var remainingInFlight = new HashSet<Key>(State.InFlightPartitions);
        remainingInFlight.Remove(partition);
        State = State with { InFlightPartitions = remainingInFlight };
        RecordedHandlingState();
    }

    ObserverSubscriberKey SubscriberKeyFor(Key partition, ObserverSubscriberTarget target)
    {
        // Webhooks have one non-interleavable activation across live delivery and jobs, independent of silo.
        var siloAddress = typeof(IWebhookObserverSubscriber).IsAssignableFrom(_subscription.SubscriberType)
            ? SiloAddress.Zero
            : target.SiloAddress;
        return _subscription.GetSubscriberKeyFor(partition, siloAddress);
    }

    bool TryTakePrefetchedDelivery(Key partition, ObserverSubscriberTarget target, [NotNullWhen(true)] out PrefetchedDelivery? prefetched)
    {
        if (!_prefetchedPartitions.Remove(partition, out prefetched)) return false;
        if (prefetched.Failure is not null) return true;
        return !CanRetryDisconnectedClient(prefetched.Result!, target);
    }

    bool CanRetryDisconnectedClient(ObserverSubscriberResult result, ObserverSubscriberTarget target) =>
        result.State == ObserverSubscriberState.Disconnected && target.ConnectedClient is not null && _subscription.Targets.Count > 1;

    void RecordHandledProgress(ObserverSubscriberResult result, AppendedEvent[] decryptedEvents, EventSequenceNumber observedTail, ref HandlingOutcome outcome)
    {
        State = State with
        {
            NextEventSequenceNumber = CursorAfter(result.LastSuccessfulObservation.Next()),
            TailEventSequenceNumber = TailAfter(observedTail)
        };
        var previousLastHandled = State.LastHandledEventSequenceNumber;
        var shouldSetLastHandled = previousLastHandled == EventSequenceNumber.Unavailable || previousLastHandled < result.LastSuccessfulObservation;
        State = State with
        {
            LastHandledEventSequenceNumber = shouldSetLastHandled ? result.LastSuccessfulObservation : previousLastHandled,
        };
        var handledEvents = decryptedEvents.Where(_ => _.Context.SequenceNumber <= result.LastSuccessfulObservation);
        outcome.HandledCounts = handledEvents.CountByEventType();
        State = WithIncrementedRunningTotals(State, outcome.HandledCounts);
        RecordedHandlingState();
    }

    Task RecordHandlingOutcome(Key partition, HandlingOutcome outcome, Activity? activity)
    {
        if (!outcome.Failed)
        {
            _metrics?.SuccessfulObservation(replay: false);
            return Task.CompletedTask;
        }

        if (outcome.FailureException is not null) activity?.RecordFailure(outcome.FailureException);
        else activity?.RecordFailure(FailureKindCode(outcome.FailureKind));
        return RecordPartitionFailure(partition, outcome.Tail, outcome.ExceptionMessages, outcome.ExceptionStackTrace, outcome.FailureKind, null, null, new(outcome.FailurePolicy, outcome.DeliveryFence));
    }
}
