// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Observation.Placement;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Reactors.Clients;

/// <summary>
/// Represents an implementation of <see cref="IReactorObserverSubscriber"/>.
/// </summary>
/// <remarks>
/// We want this grain to be local to its activation. When a client connects, the service instance that
/// receives the connection will activate this grain and we then want it to be local to that service instance
/// and not perform a network hop. The <see cref="ObserverKey"/> contains a <see cref="ConnectionId"/> which
/// will make the observer unique per connection, helping us to achieve this.
/// </remarks>
/// <param name="reactorMediator"><see cref="IReactorMediator"/> for notifying actual clients.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
/// <param name="timeProvider">The delivery clock.</param>
/// <param name="options">The telemetry configuration.</param>
[ConnectedObserverPlacement]
public class ReactorObserverSubscriber(
    IReactorMediator reactorMediator,
    ILogger<ReactorObserverSubscriber> logger,
    TimeProvider? timeProvider = null,
    IOptions<ChronicleOptions>? options = null) : Grain, IReactorObserverSubscriber, IReactorPageObserverSubscriber
{
    readonly ClientObserverSubscriberMetrics _metrics = new(options?.Value?.Telemetry.MetricNames ?? MetricNames.Legacy);
    readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    ObserverKey _key = ObserverKey.NotSet;

    /// <inheritdoc/>
    public Task Drain() => Task.CompletedTask;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        (_key, _) = this.GetKeys();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<ObserverSubscriberResult> OnNext(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context) =>
        BoundedPageDelivery.Deliver(PageBoundsFor(context), events, page => DeliverPartition(partition, page, context));

    /// <inheritdoc/>
    public Task<ObserverPageResult> OnNextPartitions(IReadOnlyList<ObserverPartitionBatch> batches, ObserverSubscriberContext context) =>
        BoundedPageDelivery.Deliver(PageBoundsFor(context), batches, page => DeliverPartitions(page, context));

    static ObserverPageResult Settled(IReadOnlyList<ObserverPartitionBatch> batches, ObserverSubscriberResult result) =>
        new(batches.ToDictionary(_ => _.Partition, _ => result));

    /// <summary>
    /// Gets the bounds of the pages the connection receives: the bounds the client declared, capped by the kernel's maximums.
    /// </summary>
    /// <param name="context">The <see cref="ObserverSubscriberContext"/> of the delivery.</param>
    /// <returns>The <see cref="PageBounds"/>; unbounded for a client that did not declare page delivery.</returns>
    PageBounds PageBoundsFor(ObserverSubscriberContext context) =>
        context.Metadata is ConnectedClient connectedClient
            ? PageBounds.For(
                reactorMediator.GetPageDelivery(_key.ObserverId, connectedClient.ConnectionId, _key.EventStore, _key.Namespace),
                options?.Value?.Observers ?? new Observers())
            : PageBounds.Unbounded;

    async Task<ObserverSubscriberResult> DeliverPartition(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        var deliveryWait = new ClientDeliveryWait(_clock, logger, _key, partition, _metrics);
        if (deliveryWait.Expired(context))
        {
            return ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Delivery expired before dispatch");
        }

        foreach (var @event in events)
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
            throw new MissingStateForReactorSubscriber(_key.ObserverId);
        }
        var tcs = new TaskCompletionSource<ObserverSubscriberResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            reactorMediator.OnNext(_key.ObserverId, connectedClient.ConnectionId, _key.EventStore, _key.Namespace, partition, events, tcs, context.RemainingDeliveryTime(_clock), context.Origin);
            var result = await deliveryWait.For(tcs.Task, result => result, context);
            return result.ResumingDeferralWithin(events);
        }
        catch (AbandonedDeliveryDiscarded)
        {
            return ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Abandoned delivery result discarded");
        }
        catch (TaskCanceledException taskCanceledException)
        {
            logger.OnNextException(taskCanceledException, _key.ObserverId, _key.EventStore, _key.Namespace);
            return ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Task was cancelled");
        }
    }

    async Task<ObserverPageResult> DeliverPartitions(IReadOnlyList<ObserverPartitionBatch> batches, ObserverSubscriberContext context)
    {
        var deliveryWait = new ClientDeliveryWait(_clock, logger, _key, ObserverSubscriberKey.AllPartitions, _metrics);
        if (deliveryWait.Expired(context))
        {
            return Settled(batches, ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Delivery expired before dispatch"));
        }

        if (context.Metadata is not ConnectedClient connectedClient)
        {
            throw new MissingStateForReactorSubscriber(_key.ObserverId);
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

        var completions = batches.ToDictionary(_ => _.Partition, _ => new TaskCompletionSource<ObserverSubscriberResult>(TaskCreationOptions.RunContinuationsAsynchronously));
        try
        {
            reactorMediator.OnNextPage(_key.ObserverId, connectedClient.ConnectionId, _key.EventStore, _key.Namespace, batches, completions, context.RemainingDeliveryTime(_clock), context.Origin);
            var results = await deliveryWait.For(Task.WhenAll(completions.Select(async _ => (_.Key, Result: await _.Value.Task))), answers => answers.Length > 0 ? answers[0].Result : ObserverSubscriberResult.Ok(EventSequenceNumber.Unavailable), context);
            return new(batches.ToDictionary(_ => _.Partition, _ => results.First(r => r.Key.Equals(_.Partition)).Result.ResumingDeferralWithin(_.Events)));
        }
        catch (AbandonedDeliveryDiscarded)
        {
            return Settled(batches, ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Abandoned delivery result discarded"));
        }
        catch (TaskCanceledException taskCanceledException)
        {
            logger.OnNextException(taskCanceledException, _key.ObserverId, _key.EventStore, _key.Namespace);
            return Settled(batches, ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Task was cancelled"));
        }
    }
}
