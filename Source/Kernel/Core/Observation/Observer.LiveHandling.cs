// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    static void RecordLiveSubscriberFailure(ref LiveHandlingOutcome outcome, ObserverSubscriberResult result, EventSequenceNumber firstEvent)
    {
        outcome.Failed = true;
        outcome.FailureKind = FailureKind.Handling;
        outcome.FailurePolicy = result.FailurePolicy;
        outcome.ExceptionMessages = result.ExceptionMessages;
        outcome.ExceptionStackTrace = result.ExceptionStackTrace;
        outcome.Tail = result.HandledAnyEvents ? result.LastSuccessfulObservation : firstEvent;
    }

    async Task<LiveHandlingOutcome> HandleLiveDelivery(LiveHandlingBatch batch)
    {
        var outcome = new LiveHandlingOutcome(batch.NextEventSequenceNumber);
        using (new WriteSuspension(this))
        {
            try
            {
                var firstEvent = batch.Events[0];
                outcome.Tail = firstEvent.Context.SequenceNumber;
                var decryptedEvents = await DecryptEvents(batch.Events);
                var delivery = await SendLiveDelivery(batch.Partition, decryptedEvents, batch.Configuration);
                outcome.Fence = delivery.Fence;
                if (delivery.Failure is not null) ExceptionDispatchInfo.Capture(delivery.Failure).Throw();
                var result = delivery.Result!;

                var numEventsSuccessfullyHandled = result.HandledAnyEvents
                    ? batch.Events.Count(_ => _.Context.SequenceNumber <= result.LastSuccessfulObservation)
                    : EventCount.Zero;

                if (result.State == ObserverSubscriberState.Deferred)
                {
                    await PartitionDeferred(batch.Partition, result.Deferral!);
                    outcome.StateChanged = true;
                }
                else if (result.State == ObserverSubscriberState.Failed)
                {
                    RecordLiveSubscriberFailure(ref outcome, result, firstEvent.Context.SequenceNumber);
                }
                else if (result.State == ObserverSubscriberState.Disconnected)
                {
                    // The batch is not advanced, so whatever subscription takes over catches up from the same position.
                    await PromoteSupersededRegistrationOrUnsubscribe();
                    outcome.StateChanged = true;
                }

                if (numEventsSuccessfullyHandled > 0)
                {
                    RecordLiveHandlingProgress(batch, result, decryptedEvents, ref outcome);
                }
            }
            catch (Exception ex)
            {
                outcome.Failed = true;
                outcome.FailureKind = ex.ToFailureKind();
                outcome.FailureException = ex;
                outcome.ExceptionMessages = ex.GetAllMessages().ToArray();
                outcome.ExceptionStackTrace = ex.StackTrace ?? string.Empty;
            }
        }

        return outcome;
    }

    async Task<LiveSubscriberDelivery> SendLiveDelivery(Key partition, AppendedEvent[] decryptedEvents, Observers configuration)
    {
        DeliveryFenceContext? deliveryFence = null;
        try
        {
            var subscriberTimeout = GetLiveSubscriberTimeout(configuration.GetSubscriberTimeout());
            var hold = await GetClientDeliveryHold();
            while (true)
            {
                var target = subscriberSelector.Select(_subscription, partition);

                // A webhook partition has one non-interleavable activation across live delivery and jobs,
                // independent of which silo owns the observer or executes the job.
                var siloAddress = typeof(IWebhookObserverSubscriber).IsAssignableFrom(_subscription.SubscriberType)
                    ? SiloAddress.Zero
                    : target.SiloAddress;
                var key = _subscription.GetSubscriberKeyFor(partition, siloAddress);

                if (_prefetchedPartitions.Remove(partition, out var prefetched) && !MustRetryPrefetchedDelivery(prefetched, target))
                {
                    // The page this partition belongs to has already been delivered; this is its settlement.
                    deliveryFence = prefetched.Fence;
                    if (prefetched.Failure is not null)
                    {
                        throw prefetched.Failure;
                    }

                    return new(prefetched.Result, deliveryFence, null);
                }

                var subscriber = (GrainFactory.GetGrain(_subscription.SubscriberType, key) as IObserverSubscriber)!;
                var context = new ObserverSubscriberContext(target.ConnectedClient ?? _subscription.Arguments) { Origin = ObservationOrigin.Live }.WithDeadlines(subscriberTimeout, hold, _clock);
                deliveryFence = AbandonedDeliveryDrain.Capture(_subscription, partition, target, context);
                var result = await subscriber.OnNextWithin(subscriberTimeout, partition, decryptedEvents, context);

                // A disconnected result from one client instance only removes that instance —
                // the batch is retried against the remaining instances. Only when the last
                // instance is gone does the observer unsubscribe.
                if (MustRetryDisconnectedDelivery(result, target))
                {
                    logger.ClientInstanceDisconnectedRetryingWithRemaining(target.ConnectedClient!.ConnectionId, partition);
                    RemoveSubscriberTarget(target);
                    continue;
                }

                return new(result, deliveryFence, null);
            }
        }
        catch (Exception ex)
        {
            // Return the fence even on failure, so the caller's existing failure path can retain it.
            return new(null, deliveryFence, ex);
        }
    }

    bool MustRetryPrefetchedDelivery(PrefetchedDelivery prefetched, ObserverSubscriberTarget target) =>
        prefetched.Failure is null && MustRetryDisconnectedDelivery(prefetched.Result!, target);

    bool MustRetryDisconnectedDelivery(ObserverSubscriberResult result, ObserverSubscriberTarget target) =>
        result.State == ObserverSubscriberState.Disconnected && target.ConnectedClient is not null && _subscription.Targets.Count > 1;

    void RecordLiveHandlingProgress(LiveHandlingBatch batch, ObserverSubscriberResult result, AppendedEvent[] decryptedEvents, ref LiveHandlingOutcome outcome)
    {
        outcome.StateChanged = true;
        State = State with
        {
            NextEventSequenceNumber = CursorBeforePending(batch.Partition, CursorAfter(result.LastSuccessfulObservation.Next())),
            TailEventSequenceNumber = TailAfter(batch.ObservedTail)
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

        CompleteHeldBackDelivery(batch.Partition, result.LastSuccessfulObservation);
        var handledEvents = decryptedEvents.Where(_ => _.Context.SequenceNumber <= result.LastSuccessfulObservation);
        outcome.HandledCountsPerEventType = handledEvents.CountByEventType();
        State = WithIncrementedRunningTotals(State, outcome.HandledCountsPerEventType);
        RecordedHandlingState();
    }

    async Task SettleLiveDelivery(Key partition, LiveHandlingOutcome outcome, Activity? activity)
    {
        try
        {
            // The in-flight marker has served its purpose now that the outcome is known, so clear it before the
            // outcome is persisted — whichever state write happens below carries the removal. A partition that
            // failed is recovered through FailedPartitions storage, so keeping its marker would only force a
            // redundant catch-up for events we already know about.
            var remainingInFlight = new HashSet<Key>(State.InFlightPartitions);
            remainingInFlight.Remove(partition);
            State = State with { InFlightPartitions = remainingInFlight };
            RecordedHandlingState();

            await RecordLiveHandlingOutcome(partition, outcome, activity);
            if (outcome.StateChanged)
            {
                await WriteStateAsync();
            }

            if (_observerExists)
            {
                await CoalesceLiveHandledCounts(partition, outcome.HandledCountsPerEventType);
            }
        }
        catch (Exception ex)
        {
            activity?.RecordFailure(ex);
            logger.ObserverFailedForUnknownReasonsAfterHandlingEvents(ex);
        }
    }

    Task RecordLiveHandlingOutcome(Key partition, LiveHandlingOutcome outcome, Activity? activity)
    {
        if (!outcome.Failed)
        {
            _metrics?.SuccessfulObservation(replay: false);
            return Task.CompletedTask;
        }

        if (outcome.FailureException is not null) activity?.RecordFailure(outcome.FailureException);
        else activity?.RecordFailure(FailureKindCode(outcome.FailureKind));
        return RecordPartitionFailure(partition, outcome.Tail, outcome.ExceptionMessages, outcome.ExceptionStackTrace, outcome.FailureKind, null, null, new(outcome.FailurePolicy, outcome.Fence));
    }

    readonly record struct LiveHandlingBatch(
        Key Partition,
        AppendedEvent[] Events,
        EventSequenceNumber ObservedTail,
        EventSequenceNumber NextEventSequenceNumber,
        Observers Configuration);

    readonly record struct LiveSubscriberDelivery(ObserverSubscriberResult? Result, DeliveryFenceContext? Fence, Exception? Failure);

    record struct LiveHandlingOutcome(EventSequenceNumber Tail)
    {
        public bool Failed { get; set; }
        public bool StateChanged { get; set; }
        public FailureKind FailureKind { get; set; } = FailureKind.Unknown;
        public Exception? FailureException { get; set; }
        public WebhookDeliveryFailurePolicy FailurePolicy { get; set; } = WebhookDeliveryFailurePolicy.Retry;
        public IEnumerable<string> ExceptionMessages { get; set; } = Enumerable.Empty<string>();
        public string ExceptionStackTrace { get; set; } = string.Empty;
        public DeliveryFenceContext? Fence { get; set; }
        public IReadOnlyDictionary<EventTypeId, EventCount> HandledCountsPerEventType { get; set; } = ImmutableDictionary<EventTypeId, EventCount>.Empty;
    }
}
