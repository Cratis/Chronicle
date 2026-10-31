// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation.Placement;
using Cratis.Chronicle.Projections;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Cratis.Chronicle.Observation.Reducers.Clients;

/// <summary>
/// Represents an implementation of <see cref="IReducerObserverSubscriber"/>.
/// </summary>
/// <remarks>
/// We want this grain to be local to its activation. When a client connects, the service instance that
/// receives the connection will activate this grain and we then want it to be local to that service instance
/// and not perform a network hop. The <see cref="ObserverSubscriberKey"/> contains the silo address
/// which ensures the grain is placed on the silo that owns the <see cref="IReducerMediator"/> subscription
/// for this client.
/// </remarks>
/// <param name="reducerPipelineFactory"><see cref="IReducerPipelineFactory"/> for creating pipelines.</param>
/// <param name="reducerMediator"><see cref="IReducerMediator"/> for notifying actual clients.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
/// <param name="timeProvider"><see cref="TimeProvider"/> for deciding when the cached pipeline is due for a refresh.</param>
[ConnectedObserverPlacement]
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Reducers)]
public class ReducerObserverSubscriber(
    IReducerPipelineFactory reducerPipelineFactory,
    IReducerMediator reducerMediator,
    ILogger<ReducerObserverSubscriber> logger,
    TimeProvider? timeProvider = null) : Grain<ReducerDefinition>, IReducerObserverSubscriber
{
    /// <summary>
    /// How long a built pipeline - and the sink it resolved the target read model's definition to - is
    /// trusted before the next <see cref="OnNext"/> rebuilds it from the read model's current definition.
    /// </summary>
    /// <remarks>
    /// This grain activates once per partition and keeps running for as long as the client stays connected -
    /// potentially the activation's entire lifetime, since nothing here ever deactivates it on its own. A
    /// read model's definition - its container name, in particular - can change on a later client reconnect
    /// (a rename) while this activation is never told: the client-side reducer re-registering its own
    /// definition is a deliberate no-op when that definition is unchanged, which it usually is, and even
    /// when it is not, re-registration resubscribes the observer rather than reaching into every partition
    /// activation's cached pipeline. Without a bound, a pipeline built against a superseded container name
    /// keeps writing to it for as long as the activation lives. A short, bounded staleness window costs one
    /// extra read-model lookup per window on an already cross-grain call; the default favors bounded
    /// staleness over added work on every batch.
    /// </remarks>
    internal static readonly TimeSpan PipelineRefreshInterval = TimeSpan.FromMinutes(1);

    readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    ObserverKey _key = ObserverKey.NotSet;
    IReducerPipeline? _pipeline;
    IProjectionChangesetNotifier _changesetNotifier = default!;
    DateTimeOffset _pipelineBuiltAt = DateTimeOffset.MinValue;

    /// <inheritdoc/>
    public Task Drain() => Task.CompletedTask;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        (_key, _) = this.GetKeys();
        _changesetNotifier = GrainFactory.GetGrain<IProjectionChangesetNotifier>(_key.ObserverId);
        await HandlePipeline();
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        var deliveryWait = new ClientDeliveryWait(_clock, logger, _key, partition);
        if (!await PrepareDelivery(deliveryWait, context))
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
            throw new MissingStateForReducerSubscriber(_key.ObserverId);
        }

        var tcs = new TaskCompletionSource<ObserverSubscriberResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var reducerContext = new ReducerContext(
                events,
                partition);

            var notification = _pipeline is null ? null : await _pipeline.Reduce(reducerContext, CreateReducerDelegate(reducerContext, connectedClient, context, tcs));
            return await NotifyChange(await tcs.Task, notification);
        }
        catch (AbandonedDeliveryDiscarded)
        {
            return ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Abandoned delivery result discarded");
        }
        catch (TaskCanceledException)
        {
            return ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Task was cancelled");
        }
        catch (ReducerSinkWriteFailed failed)
        {
            return await RecordSinkWriteFailures(partition, failed, tcs.Task);
        }
    }

    /// <summary>
    /// Reports only what the read model actually holds.
    /// </summary>
    /// <remarks>
    /// A reducer folds a whole batch into one state, and the pipeline writes that state only for a successful result.
    /// A failed result can still report a successful prefix of the batch as handled, but nothing of that prefix was
    /// written. Passing it on would move the observer past events the read model never received, and recovery would
    /// then start from the failing event on top of the stale state - losing the prefix's fold for good. Reporting no
    /// events as handled makes the observer record the failure at the batch's first event, so every path that drives
    /// this subscriber - live handling, catch-up, replay and retry - starts the batch over from the stored state.
    /// </remarks>
    /// <param name="result">The <see cref="ObserverSubscriberResult"/> the client reported.</param>
    /// <returns>The <see cref="ObserverSubscriberResult"/> to report to the observer.</returns>
    static ObserverSubscriberResult AsHandledByThePipeline(ObserverSubscriberResult result) =>
        result.State == ObserverSubscriberState.Failed
            ? result with { LastSuccessfulObservation = EventSequenceNumber.Unavailable }
            : result;

    async Task<ObserverSubscriberResult> NotifyChange(ObserverSubscriberResult result, ReadModelChangeNotification? notification)
    {
        if (result.State != ObserverSubscriberState.Ok || notification is null) return result;

        try
        {
            await _changesetNotifier.Notify(_key.Namespace, notification.ReadModelKey, notification.ReadModel, notification.Change);
            return result;
        }
        catch (Exception ex)
        {
            logger.NotificationFailed(ex, _key.ObserverId, _key.EventStore, _key.Namespace);
            return result;
        }
    }

    async Task<bool> PrepareDelivery(ClientDeliveryWait deliveryWait, ObserverSubscriberContext context)
    {
        if (deliveryWait.Expired(context)) return false;
        if (_clock.GetUtcNow() - _pipelineBuiltAt >= PipelineRefreshInterval)
        {
            await HandlePipeline();
        }
        return true;
    }

    ReducerDelegate CreateReducerDelegate(ReducerContext reduction, ConnectedClient client, ObserverSubscriberContext context, TaskCompletionSource<ObserverSubscriberResult> completion) =>
        async (events, initialState) =>
        {
            var deliveryWait = new ClientDeliveryWait(_clock, logger, _key, reduction.Key);

            // Pipeline setup can itself outlive the caller's deadline. Never dispatch a queued ghost.
            if (deliveryWait.Expired(context))
            {
                throw new AbandonedDeliveryDiscarded();
            }
            var reducerSubscriberResultTCS = new TaskCompletionSource<ReducerSubscriberResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            reducerMediator.OnNext(
                _key.ObserverId,
                client.ConnectionId,
                _key.EventStore,
                _key.Namespace,
                new(reduction.Key, events, initialState) { DeliverWithin = context.RemainingDeliveryTime(_clock) },
                reducerSubscriberResultTCS);

            var result = await deliveryWait.For(reducerSubscriberResultTCS.Task, result => result.ObserverResult, context);
            completion.SetResult(AsHandledByThePipeline(result.ObserverResult));
            return result;
        };

    /// <summary>
    /// Records every partition a sink write reported as failed.
    /// </summary>
    /// <remarks>
    /// A bulk flush can drop other partitions' writes along with this one's. Those are recorded as failed so they are
    /// retried; this partition fails only when its own write is among them, otherwise its result stands.
    /// </remarks>
    /// <param name="partition">The partition being reduced.</param>
    /// <param name="failed">The <see cref="ReducerSinkWriteFailed"/> carrying the failures.</param>
    /// <param name="result">The result the client reported for this partition.</param>
    /// <returns>The <see cref="ObserverSubscriberResult"/> to report to the observer.</returns>
    async Task<ObserverSubscriberResult> RecordSinkWriteFailures(Key partition, ReducerSinkWriteFailed failed, Task<ObserverSubscriberResult> result)
    {
        var others = failed.FailedPartitions.Where(failure => !Equals(failure.EventSourceId.Value, partition.Value)).ToArray();
        if (others.Length > 0)
        {
            var observer = GrainFactory.GetGrain<IObserver>(new ObserverKey(_key.ObserverId, _key.EventStore, _key.Namespace, _key.EventSequenceId));
            foreach (var failure in others)
            {
                await observer.PartitionFailed(
                    failure.EventSourceId,
                    failure.EventSequenceNumber,
                    [ReducerSinkWriteFailed.MessageFor(failure)],
                    string.Empty,
                    FailureKind.Handling);
            }
        }

        return others.Length == failed.FailedPartitions.Count && result.IsCompleted
            ? await result
            : ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, failed.Message);
    }

    async Task HandlePipeline()
    {
        _pipeline = await reducerPipelineFactory.Create(_key.EventStore, _key.Namespace, State);
        _pipelineBuiltAt = _clock.GetUtcNow();
    }
}
