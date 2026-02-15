// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;
using NJsonSchema;
using Orleans.Providers;
using Orleans.Streams;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionObserverSubscriber"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionObserverSubscriber"/> class.
/// </remarks>
/// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating projections.</param>
/// <param name="projectionPipelineManager"><see cref="IProjectionPipelineManager"/> for creating projection pipelines.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting to and from <see cref="ExpandoObject"/>.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing storage.</param>
/// <param name="logger">The logger.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Projections)]
public class ProjectionObserverSubscriber(
    IProjectionFactory projectionFactory,
    IProjectionPipelineManager projectionPipelineManager,
    IExpandoObjectConverter expandoObjectConverter,
    IStorage storage,
    ILogger<ProjectionObserverSubscriber> logger) : Grain<ProjectionDefinition>, IProjectionObserverSubscriber, INotifyProjectionDefinitionsChanged
{
    ObserverSubscriberKey _key = ObserverSubscriberKey.Unspecified;
    IProjectionPipeline? _pipeline;
    IAsyncStream<ProjectionChangeset>? _changeStream;
    JsonSchema? _schema;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var streamProvider = this.GetStreamProvider(WellKnownStreamProviders.ProjectionChangesets);
        (_, _key) = this.GetKeys();

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
    public async Task OnProjectionDefinitionsChanged(ProjectionDefinition definition)
    {
        // Update state with the new definition
        State = definition;
        await WriteStateAsync();

        // Rebuild the pipeline with the updated definition
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
                var pipelineContext = await _pipeline.Handle(@event);
                changeset = pipelineContext.Changeset;

                // Check if there are any failed partitions from bulk operations
                if (pipelineContext.FailedPartitions.Any())
                {
                    var observer = GrainFactory.GetGrain<IObserver>(new ObserverKey(_key.ObserverId, _key.EventStore, _key.Namespace, _key.EventSequenceId));
                    foreach (var failedPartition in pipelineContext.FailedPartitions)
                    {
                        await observer.PartitionFailed(
                            failedPartition.EventSourceId,
                            failedPartition.EventSequenceNumber,
                            [$"Bulk operation failed for partition {failedPartition.EventSourceId}"],
                            string.Empty);
                    }
                }

                lastSuccessfullyObservedEvent = @event;
                logger.SuccessfullyHandledEvent(@event.Context.SequenceNumber, _key);
            }

            // Note: We don't want to send changesets if the projection is not active
            if (changeset?.HasChanges == true && State.IsActive)
            {
                var model = expandoObjectConverter.ToJsonObject(changeset.CurrentState, _schema!);
                await _changeStream!.OnNextAsync(new ProjectionChangeset(_key.Namespace, partition.ToString(), model));
            }

            logger.SuccessfullyHandledAllEvents(_key);
            return ObserverSubscriberResult.Ok(lastSuccessfullyObservedEvent!.Context.SequenceNumber);
        }
        catch (Exception ex)
        {
            logger.ErrorHandling(ex, _key, lastSuccessfullyObservedEvent?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable);
            return new(
                ObserverSubscriberState.Failed,
                lastSuccessfullyObservedEvent?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable,
                ex.GetAllMessages(),
                ex.StackTrace ?? string.Empty);
        }
    }

    async Task HandlePipeline()
    {
        if (State.ReadModel is null)
        {
            return;
        }

        var readModel = await GrainFactory.GetGrain<IReadModel>(new ReadModelGrainKey(State.ReadModel, _key.EventStore)).GetDefinition();
        var eventStoreStorage = storage.GetEventStore(_key.EventStore);
        var eventTypeSchemas = await eventStoreStorage.EventTypes.GetLatestForAllEventTypes();
        var projection = await projectionFactory.Create(_key.EventStore, _key.Namespace, State, readModel, eventTypeSchemas);
        _pipeline = projectionPipelineManager.GetFor(_key.EventStore, _key.Namespace, projection);
        _schema = readModel.GetSchemaForLatestGeneration();
    }
}
