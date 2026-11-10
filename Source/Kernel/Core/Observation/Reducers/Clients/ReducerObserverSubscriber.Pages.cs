// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Reducers.Clients;

/// <summary>
/// Partial for reducing a page of partitions spanning event sources in one delivery and settling each partition on its own.
/// </summary>
public partial class ReducerObserverSubscriber
{
    /// <inheritdoc/>
    public async Task<ObserverPageResult> OnNextPartitions(IReadOnlyList<ObserverPartitionBatch> batches, ObserverSubscriberContext context) =>
        await BoundedPageDelivery.Deliver(await PageBoundsFor(context), batches, page => DeliverPartitions(page, context), StateSizeOf);

    static ObserverPageResult Settled(IReadOnlyList<ObserverPartitionBatch> batches, ObserverSubscriberResult result) =>
        new(batches.ToDictionary(_ => _.Partition, _ => result));

    /// <summary>
    /// Gets the result a late answer to a page is logged and counted with.
    /// </summary>
    /// <remarks>
    /// Logging and metrics of a late answer take one state, but a page has one per partition. The first partition's result
    /// stands in for the page: the choice is arbitrary and only affects what is logged, never what is settled.
    /// </remarks>
    /// <param name="answers">The answer of each partition in the page.</param>
    /// <returns>The <see cref="ObserverSubscriberResult"/> to log and count the late answer with.</returns>
    static ObserverSubscriberResult StateReportedForPage((Key Key, ReducerSubscriberResult Result)[] answers) =>
        answers.Length > 0 ? answers[0].Result.ObserverResult : ObserverSubscriberResult.Ok(EventSequenceNumber.Unavailable);

    async Task<ObserverPageResult> DeliverPartitions(IReadOnlyList<ObserverPartitionBatch> batches, ObserverSubscriberContext context)
    {
        var deliveryWait = new ClientDeliveryWait(_clock, logger, _key, ObserverSubscriberKey.AllPartitions, _metrics);
        if (!await PrepareDelivery(deliveryWait, context))
        {
            return Settled(batches, ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Delivery expired before dispatch"));
        }

        foreach (var @event in batches.SelectMany(_ => _.Events))
        {
            logger.EventReceived(
                _key.ObserverId,
                _key.EventStore,
                _key.Namespace,
                @event.Context.EventType.Id,
                _key.EventSequenceId,
                @event.Context.SequenceNumber);
        }

        if (context.Metadata is not ConnectedClient connectedClient)
        {
            throw new MissingStateForReducerSubscriber(_key.ObserverId);
        }

        if (_pipeline is null)
        {
            return Settled(batches, ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "The reducer pipeline is not available"));
        }

        try
        {
            var contexts = batches.Select(_ => new ReducerContext(_.Events, _.Partition)).ToArray();
            var outcome = await _pipeline.ReducePage(contexts, CreatePageReducerDelegate(connectedClient, context), await IsWatched());
            return await Settle(batches, outcome);
        }
        catch (AbandonedDeliveryDiscarded)
        {
            return Settled(batches, ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Abandoned delivery result discarded"));
        }
        catch (TaskCanceledException)
        {
            return Settled(batches, ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Task was cancelled"));
        }
    }

    ReducerPageDelegate CreatePageReducerDelegate(ConnectedClient client, ObserverSubscriberContext context) =>
        async partitions =>
        {
            var deliveryWait = new ClientDeliveryWait(_clock, logger, _key, ObserverSubscriberKey.AllPartitions, _metrics);

            // Reading the page's states can itself outlive the caller's deadline. Never dispatch a queued ghost.
            if (deliveryWait.Expired(context))
            {
                throw new AbandonedDeliveryDiscarded();
            }

            var deliverWithin = context.RemainingDeliveryTime(_clock);
            var operations = partitions
                .Select(_ => new ReduceOperation(_.Key, _.Events, _.InitialState) { DeliverWithin = deliverWithin, Origin = context.Origin })
                .ToArray();
            var completions = operations.ToDictionary(_ => _.Partition, _ => new TaskCompletionSource<ReducerSubscriberResult>(TaskCreationOptions.RunContinuationsAsynchronously));
            reducerMediator.OnNextPage(_key.ObserverId, client.ConnectionId, _key.EventStore, _key.Namespace, operations, completions);

            // The client answers the page as a whole, so the page waits for every partition. One partition the client never
            // answers holds the page until the delivery's deadline, and the whole page is then abandoned: every partition
            // fails and is delivered again, the ones the client did answer included, since nothing of the page is written.
            var answers = await deliveryWait.For(
                Task.WhenAll(completions.Select(async _ => (_.Key, Result: await _.Value.Task))),
                StateReportedForPage,
                context);
            return answers.ToDictionary(_ => _.Key, _ => _.Result);
        };

    /// <summary>
    /// Settles each partition of a page on its own.
    /// </summary>
    /// <remarks>
    /// A partition the client failed or deferred wrote nothing, so it is reported as handling nothing and is delivered again from
    /// its stored state, exactly as on its own. A partition whose write failed fails alone, and failed writes a bulk window the page
    /// joined reported for partitions outside the page are recorded so they are retried.
    /// </remarks>
    /// <param name="batches">The partition batches of the page.</param>
    /// <param name="outcome">The <see cref="ReducerPageOutcome"/> of the page.</param>
    /// <returns>The <see cref="ObserverPageResult"/> with one result per partition.</returns>
    async Task<ObserverPageResult> Settle(IReadOnlyList<ObserverPartitionBatch> batches, ReducerPageOutcome outcome)
    {
        bool InPage(Key key) => batches.Any(batch => Equals(batch.Partition.Value, key.Value));
        await RecordFailedPartitions([.. outcome.FailedPartitions.Where(failure => !InPage(failure.EventSourceId))]);

        var results = new Dictionary<Key, ObserverSubscriberResult>();
        foreach (var batch in batches)
        {
            var writeFailures = outcome.FailedPartitions.Where(failure => Equals(failure.EventSourceId.Value, batch.Partition.Value)).ToArray();
            results[batch.Partition] = (outcome.Results.TryGetValue(batch.Partition, out var reduced), writeFailures.Length) switch
            {
                (false, _) => ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "The client did not answer for the partition"),
                (true, > 0) => ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, new ReducerSinkWriteFailed(writeFailures).Message),
                _ => AsHandledByThePipeline(reduced!.ObserverResult, batch.Events)
            };

            if (results[batch.Partition].State == ObserverSubscriberState.Ok && outcome.Notifications.TryGetValue(batch.Partition, out var notification))
            {
                results[batch.Partition] = await NotifyChange(results[batch.Partition], notification);
            }
        }

        return new(results);
    }
}
