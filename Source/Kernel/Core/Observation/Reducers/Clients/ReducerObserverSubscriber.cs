// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Observation.Placement;
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
    DateTimeOffset _pipelineBuiltAt = DateTimeOffset.MinValue;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        (_key, _) = this.GetKeys();
        await HandlePipeline();
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        if (_clock.GetUtcNow() - _pipelineBuiltAt >= PipelineRefreshInterval)
        {
            await HandlePipeline();
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

            await (_pipeline?.Reduce(reducerContext, async (events, initialState) =>
            {
                var reducerSubscriberResultTCS = new TaskCompletionSource<ReducerSubscriberResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                reducerMediator.OnNext(
                    _key.ObserverId,
                    connectedClient.ConnectionId,
                    _key.EventStore,
                    _key.Namespace,
                    new(partition, events, initialState),
                    reducerSubscriberResultTCS);

                var result = await reducerSubscriberResultTCS.Task;
                tcs.SetResult(result.ObserverResult);
                return result;
            }) ?? Task.CompletedTask);

            return await tcs.Task;
        }
        catch (TaskCanceledException)
        {
            return ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Task was cancelled");
        }
    }

    async Task HandlePipeline()
    {
        _pipeline = await reducerPipelineFactory.Create(_key.EventStore, _key.Namespace, State);
        _pipelineBuiltAt = _clock.GetUtcNow();
    }
}
