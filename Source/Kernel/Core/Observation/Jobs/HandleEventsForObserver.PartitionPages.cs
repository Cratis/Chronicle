// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;
using OneOf.Types;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Partial for walking the event sequence once for an observer that takes pages spanning partitions, and settling each partition on its own.
/// </summary>
public partial class HandleEventsForObserver
{
    async Task<(JobStepResult? Result, EventSequenceNumber LastSuccessfullyHandledEventSequenceNumber)> HandlePartitionPage(
        HandleEventsForObserverState currentState,
        AppendedEvent[] events,
        TimeSpan subscriberTimeout,
        HashSet<Key> failedPartitions,
        EventSequenceNumber lastSuccessfullyHandledEventSequenceNumber,
        CancellationToken cancellationToken)
    {
        var batches = events
            .GroupBy(_ => _.Context.EventSourceId)
            .Select(group => new ObserverPartitionBatch(group.Key, [.. group.OrderBy(_ => _.Context.SequenceNumber)]))
            .ToList();
        if (batches.Count == 0)
        {
            return (null, lastSuccessfullyHandledEventSequenceNumber);
        }

        logger.DeliveringPage(events.Length, batches.Count);
        var dispatched = false;
        var delivery = await TryHandlePartitionPage(batches, subscriberTimeout, () => dispatched = true);
        if (delivery.TryGetException(out var exception))
        {
            return (await HandlePageDispatchException(events[0], exception), lastSuccessfullyHandledEventSequenceNumber);
        }

        if (!delivery.TryGetResult(out var page))
        {
            return (null, lastSuccessfullyHandledEventSequenceNumber);
        }

        if (!dispatched)
        {
            return (null, lastSuccessfullyHandledEventSequenceNumber);
        }

        // Partitions that failed settle first, so every parked partition is durable before the checkpoint moves past its events,
        // and the ones that succeeded settle in sequence order so the checkpoint only ever moves forward.
        var settled = page.Batches
            .Select(batch => (Batch: batch, Result: page.Result.Results.TryGetValue(batch.Partition, out var result)
                ? result
                : ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "The subscriber did not answer for the partition")))
            .OrderBy(_ => _.Result.State == ObserverSubscriberState.Ok ? 1 : 0)
            .ThenBy(_ => _.Result.LastSuccessfulObservation.IsActualValue ? _.Result.LastSuccessfulObservation.Value : ulong.MaxValue)
            .ToList();

        foreach (var (batch, result) in settled)
        {
            var handledEvents = batch.Events.ToArray();
            if (result.State is ObserverSubscriberState.Failed)
            {
                var handledCount = result.HandledAnyEvents ? handledEvents.Count(_ => _.Context.SequenceNumber <= result.LastSuccessfulObservation) : 0;
                var (failedResult, last) = await HandleFailedSubscriberResult(
                    currentState,
                    batch.Partition,
                    result,
                    handledEvents,
                    (EventCount)(ulong)handledCount,
                    lastSuccessfullyHandledEventSequenceNumber,
                    cancellationToken);
                lastSuccessfullyHandledEventSequenceNumber = last;

                // The partition is parked and its recovery belongs to the retry job, so the others in the page carry on.
                if (_stepClosed)
                {
                    return (failedResult, lastSuccessfullyHandledEventSequenceNumber);
                }

                failedPartitions.Add(batch.Partition);
                _partitionFailedInSweep = true;
                continue;
            }

            var (stepResult, newLast) = await HandleSubscriberResult(currentState, batch.Partition, result, handledEvents, lastSuccessfullyHandledEventSequenceNumber, cancellationToken);
            lastSuccessfullyHandledEventSequenceNumber = newLast;
            if (stepResult is not null)
            {
                return (stepResult, lastSuccessfullyHandledEventSequenceNumber);
            }
        }

        return (null, lastSuccessfullyHandledEventSequenceNumber);
    }

    async Task<Catch<(ObserverPageResult Result, ObserverPartitionBatch[] Batches), None>> TryHandlePartitionPage(
        List<ObserverPartitionBatch> batches,
        TimeSpan subscriberTimeout,
        Action onDispatch)
    {
        try
        {
            var decrypted = new List<ObserverPartitionBatch>();
            foreach (var batch in batches)
            {
                decrypted.Add(batch with { Events = await DecryptEvents(batch.Events) });
            }

            var target = subscriberSelector.Select(_subscription, batches[0].Partition);
            var subscriber = (IObserverPartitionPageSubscriber)grainFactory.GetGrain(_subscription.SubscriberType, _subscription.GetSubscriberKeyFor(ObserverSubscriberKey.AllPartitions, target.SiloAddress));
            var subscriberContext = new ObserverSubscriberContext(target.ConnectedClient ?? _subscription.Arguments) { Origin = _origin }.WithDeadlines(subscriberTimeout, _abandonedDeliveryHold, _clock);
            _deliveryFence = AbandonedDeliveryDrain.Capture(_subscription, ObserverSubscriberKey.AllPartitions, target, subscriberContext);
            onDispatch();
            var result = await subscriber.OnNextPartitionsWithin(subscriberTimeout, decrypted, subscriberContext);
            return (result, decrypted.ToArray());
        }
        catch (Exception ex)
        {
            logger.ErrorHandling(ex, ObserverSubscriberKey.AllPartitions);
            return ex;
        }
    }
}
