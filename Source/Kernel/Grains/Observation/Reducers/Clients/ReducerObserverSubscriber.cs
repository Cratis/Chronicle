// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Properties;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.Observation.Reducers.Clients;

/// <summary>
/// Represents an implementation of <see cref="IReducerObserverSubscriber"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReducerObserverSubscriber"/> class.
/// </remarks>
/// <param name="reducerPipelineFactory"><see cref="IReducerPipelineFactory"/> for creating pipelines.</param>
/// <param name="reducerMediator"><see cref="IReducerMediator"/> for notifying actual clients.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Reducers)]
public class ReducerObserverSubscriber(
    IReducerPipelineFactory reducerPipelineFactory,
    IReducerMediator reducerMediator,
    ILogger<ReducerObserverSubscriber> logger) : Grain<ReducerDefinition>, IReducerObserverSubscriber
{
    ObserverSubscriberKey _key = new(ObserverId.Unspecified, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, EventSequenceId.Unspecified, EventSourceId.Unspecified, string.Empty);
    EventStoreName _eventStore = EventStoreName.NotSet;
    ObserverId _observerId = ObserverId.Unspecified;
    EventStoreNamespaceName _namespace = EventStoreNamespaceName.NotSet;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;
    IReducerPipeline? _pipeline;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _key = ObserverSubscriberKey.Parse(this.GetPrimaryKeyString());
        _observerId = _key.ObserverId;
        _eventStore = _key.EventStore;
        _namespace = _key.Namespace;
        _eventSequenceId = _key.EventSequenceId;

        await HandlePipeline();
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        await Task.CompletedTask;

        foreach (var @event in events)
        {
            logger.EventReceived(
                _observerId,
                _eventStore,
                _namespace,
                @event.Metadata.Type.Id,
                _eventSequenceId,
                @event.Context.SequenceNumber);
        }

        if (context.Metadata is not ConnectedClient connectedClient)
        {
            throw new MissingStateForReducerSubscriber(_observerId);
        }

        var tcs = new TaskCompletionSource<ObserverSubscriberResult>();
        try
        {
            var firstEvent = events.First();
            var reducerContext = new ReducerContext(
                events,
                new Key(firstEvent.Context.EventSourceId, ArrayIndexers.NoIndexers));

            await (_pipeline?.Handle(reducerContext, async (events, initialState) =>
            {
                var reducerSubscriberResultTCS = new TaskCompletionSource<ReducerSubscriberResult>();

                reducerMediator.OnNext(
                    _observerId,
                    connectedClient.ConnectionId,
                    new(events, initialState),
                    reducerSubscriberResultTCS);

                await reducerSubscriberResultTCS.Task.WaitAsync(TimeSpan.FromSeconds(5));
                var result = await reducerSubscriberResultTCS.Task;
                tcs.SetResult(result.ObserverResult);
                return result.ModelState;
            }) ?? Task.CompletedTask);

            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
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
        _pipeline = await reducerPipelineFactory.Create(_eventStore, _namespace, State);
    }
}
