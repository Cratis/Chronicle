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
/// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating projections.</param>
/// <param name="projectionPipelineManager"><see cref="IProjectionPipelineManager"/> for creating projection pipelines.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting to and from <see cref="ExpandoObject"/>.</param>
/// <param name="logger">The logger.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Projections)]
public class ProjectionObserverSubscriber(
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
        IObserver? observer = null;
        try
        {
            IChangeset<AppendedEvent, ExpandoObject>? changeset = null;
            ProjectionEventContext? lastPipelineContext = null;

            foreach (var @event in events)
            {
                var pipelineContext = await _pipeline.Handle(@event);
                changeset = pipelineContext.Changeset;
                lastPipelineContext = pipelineContext;

                // Check for failed partitions after processing each event
                if (pipelineContext.HasFailedPartitions)
                {
                    observer ??= GrainFactory.GetGrain<IObserver>(new ObserverKey(_key.ObserverId, _key.EventStore, _key.Namespace, _key.EventSequenceId));

                    foreach (var failedPartition in pipelineContext.FailedPartitions)
                    {
                        // Set the ObserverId which is not known in the pipeline
                        failedPartition.ObserverId = _key.ObserverId;

                        await observer.PartitionFailed(
                            failedPartition.Partition,
                            failedPartition.LastAttempt.SequenceNumber,
                            failedPartition.LastAttempt.Messages,
                            failedPartition.LastAttempt.StackTrace);
                    }

                    // Return failed status with the last attempt information from the last failed partition
                    var lastFailedPartition = pipelineContext.FailedPartitions.Last();
                    return new(
                        ObserverSubscriberState.Failed,
                        lastSuccessfullyObservedEvent?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable,
                        lastFailedPartition.LastAttempt.Messages,
                        lastFailedPartition.LastAttempt.StackTrace);
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
        var readModel = await GrainFactory.GetGrain<IReadModel>(new ReadModelGrainKey(State.ReadModel, _key.EventStore)).GetDefinition();
        var projection = await projectionFactory.Create(_key.EventStore, _key.Namespace, State, readModel);
        _pipeline = projectionPipelineManager.GetFor(_key.EventStore, _key.Namespace, projection);
        _schema = readModel.GetSchemaForLatestGeneration();
    }
}
