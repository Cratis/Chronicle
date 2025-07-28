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
using Cratis.Chronicle.Grains.ReadModels;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Pipelines;
using Microsoft.Extensions.Logging;
using NJsonSchema;
using Orleans.Providers;
using Orleans.Streams;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionObserverSubscriber"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionObserverSubscriber"/> class.
/// </remarks>
/// <param name="projectionManager"><see cref="Chronicle.Projections.IProjectionsManager"/> for getting projections.</param>
/// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating projections.</param>
/// <param name="projectionPipelineManager"><see cref="IProjectionPipelineManager"/> for creating projection pipelines.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting to and from <see cref="ExpandoObject"/>.</param>
/// <param name="logger">The logger.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Projections)]
public class ProjectionObserverSubscriber(
    Chronicle.Projections.IProjectionsManager projectionManager,
    IProjectionFactory projectionFactory,
    IProjectionPipelineManager projectionPipelineManager,
    IExpandoObjectConverter expandoObjectConverter,
    ILogger<ProjectionObserverSubscriber> logger) : Grain<ProjectionDefinition>, IProjectionObserverSubscriber, INotifyProjectionDefinitionsChanged
{
    ObserverSubscriberKey _key = new(ObserverId.Unspecified, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, EventSequenceId.Unspecified, EventSourceId.Unspecified, string.Empty);
    IProjectionPipeline? _pipeline;
    IAsyncStream<ProjectionChangeset>? _changeStream;
    JsonSchema? _schema;

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

            // Note: We don't want to send changesets if the projection is not active
            if (changeset?.HasChanges == true && State.IsActive)
            {
                var model = expandoObjectConverter.ToJsonObject(changeset.CurrentState, _schema!);
                await _changeStream!.OnNextAsync(new ProjectionChangeset(_key.Namespace, partition.ToString(), model));
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
        var readModel = await GrainFactory.GetGrain<IReadModel>(new ReadModelGrainKey(State.ReadModel, _key.EventStore)).GetDefinition();
        if (!projectionManager.TryGet(_key.EventStore, _key.Namespace, _key.ObserverId, out var projection))
        {
            projection = await projectionFactory.Create(_key.EventStore, _key.Namespace, State, readModel);
        }
        _pipeline = projectionPipelineManager.GetFor(_key.EventStore, _key.Namespace, projection);
        _schema = readModel.Schemas;
    }
}
