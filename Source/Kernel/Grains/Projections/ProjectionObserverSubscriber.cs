// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Pipelines;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Orleans.Streams;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionObserverSubscriber"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionObserverSubscriber"/> class.
/// </remarks>
/// <param name="projectionManager"><see cref="IProjectionManager"/> for getting projections.</param>
/// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating projections.</param>
/// <param name="projectionPipelineManager"><see cref="IProjectionPipelineManager"/> for creating projection pipelines.</param>
/// <param name="logger">The logger.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Projections)]
public class ProjectionObserverSubscriber(
    IProjectionManager projectionManager,
    IProjectionFactory projectionFactory,
    IProjectionPipelineManager projectionPipelineManager,
    ILogger<ProjectionObserverSubscriber> logger) : Grain<ProjectionDefinition>, IProjectionObserverSubscriber, INotifyProjectionDefinitionsChanged
{
    ObserverSubscriberKey _key = new(ObserverId.Unspecified, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, EventSequenceId.Unspecified, EventSourceId.Unspecified, string.Empty);
    IProjectionPipeline? _pipeline;
    IAsyncStream<ProjectionChangeset>? _changeStream;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var streamProvider = this.GetStreamProvider(WellKnownStreamProviders.ProjectionChangesets);
        _key = ObserverSubscriberKey.Parse(this.GetPrimaryKeyString());

        var streamId = StreamId.Create(new StreamIdentity(Guid.Empty, _key.ObserverId));
        _changeStream = streamProvider.GetStream<ProjectionChangeset>(streamId);

        var projection = GrainFactory.GetGrain<IProjection>(new ProjectionKey(_key.ObserverId, _key.EventStore));
        await projection.SubscribeDefinitionsChanged(this.AsReference<INotifyProjectionDefinitionsChanged>());
        await HandlePipeline();
    }

    /// <inheritdoc/>
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        var projection = GrainFactory.GetGrain<IProjection>(new ProjectionKey(_key.ObserverId, _key.EventStore));
        await projection.UnsubscribeDefinitionsChanged(this.AsReference<INotifyProjectionDefinitionsChanged>());
    }

    /// <inheritdoc/>
    public async Task OnProjectionDefinitionsChanged()
    {
        await ReadStateAsync();
        await HandlePipeline();
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        if (_pipeline is null)
        {
            logger.PipelineDisconnected(_key);
            return ObserverSubscriberResult.Disconnected();
        }

        AppendedEvent? lastSuccessfullyObservedEvent = default;
        try
        {
            IChangeset<AppendedEvent, ExpandoObject>? changeset = null;

            foreach (var @event in events)
            {
                changeset = await _pipeline.Handle(@event);
                lastSuccessfullyObservedEvent = @event;
                logger.SuccessfullyHandledEvent(@event.Metadata.SequenceNumber, _key);
            }

            if (changeset is not null)
            {
                await _changeStream!.OnNextAsync(new ProjectionChangeset(_key.Namespace, partition.ToString(), changeset.CurrentState));
            }

            logger.SuccessfullyHandledAllEvents(_key);
            return ObserverSubscriberResult.Ok(lastSuccessfullyObservedEvent!.Metadata.SequenceNumber);
        }
        catch (Exception ex)
        {
            logger.ErrorHandling(ex, _key, lastSuccessfullyObservedEvent?.Metadata.SequenceNumber ?? EventSequenceNumber.Unavailable);
            return new(
                ObserverSubscriberState.Failed,
                lastSuccessfullyObservedEvent?.Metadata.SequenceNumber ?? EventSequenceNumber.Unavailable,
                ex.GetAllMessages(),
                ex.StackTrace ?? string.Empty);
        }
    }

    async Task HandlePipeline()
    {
        if (!projectionManager.TryGet(_key.EventStore, _key.Namespace, _key.ObserverId, out var projection))
        {
            projection = await projectionFactory.Create(_key.EventStore, _key.Namespace, State);
        }
        _pipeline = projectionPipelineManager.GetFor(_key.EventStore, _key.Namespace, projection);
    }
}
