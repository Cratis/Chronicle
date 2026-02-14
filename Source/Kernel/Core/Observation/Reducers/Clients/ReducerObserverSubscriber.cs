// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Cratis.Chronicle.Observation.Reducers.Clients;

/// <summary>
/// Represents an implementation of <see cref="IReducerObserverSubscriber"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReducerObserverSubscriber"/> class.
/// </remarks>
/// <param name="reducerPipelineFactory"><see cref="IReducerPipelineFactory"/> for creating pipelines.</param>
/// <param name="reducerMediator"><see cref="IReducerMediator"/> for notifying actual clients.</param>
/// <param name="configurationProvider"><see cref="IConfigurationForObserverProvider"/> for providing <see cref="Observers"/> config.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Reducers)]
public class ReducerObserverSubscriber(
    IReducerPipelineFactory reducerPipelineFactory,
    IReducerMediator reducerMediator,
    IConfigurationForObserverProvider configurationProvider,
    ILogger<ReducerObserverSubscriber> logger) : Grain<ReducerDefinition>, IReducerObserverSubscriber
{
    ObserverKey _key = ObserverKey.NotSet;
    IReducerPipeline? _pipeline;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        (_key, _) = this.GetKeys();
        await HandlePipeline();
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
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

            var timeout = await configurationProvider.GetSubscriberTimeoutForObserver(_key);
            await (_pipeline?.Handle(reducerContext, async (events, initialState) =>
            {
                var reducerSubscriberResultTCS = new TaskCompletionSource<ReducerSubscriberResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                reducerMediator.OnNext(
                    _key.ObserverId,
                    connectedClient.ConnectionId,
                    new(partition, events, initialState),
                    reducerSubscriberResultTCS);

                await reducerSubscriberResultTCS.Task.WaitAsync(timeout);
                var result = await reducerSubscriberResultTCS.Task;
                tcs.SetResult(result.ObserverResult);
                return result;
            }) ?? Task.CompletedTask);

            await tcs.Task.WaitAsync(timeout);
            return await tcs.Task;
        }
        catch (TaskCanceledException)
        {
            return ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Task was cancelled");
        }
        catch (TimeoutException)
        {
            return ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Timeout");
        }
    }

    async Task HandlePipeline()
    {
        _pipeline = await reducerPipelineFactory.Create(_key.EventStore, _key.Namespace, State);
    }
}
