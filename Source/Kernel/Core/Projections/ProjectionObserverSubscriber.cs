// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;
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

        // Always fetch the current definition from the IProjection grain rather than relying on
        // the stored State. The stored State can be stale when this grain re-activates after
        // a deactivation that occurred while the projection definition was being updated (for
        // example, during an application restart). IProjection.State is always authoritative
        // because it is persisted before the replay is started.
        var currentDefinition = await projection.GetDefinition();
        if (currentDefinition.ReadModel is not null)
        {
            // Check if definition has changed
            var definitionChanged = HasDefinitionChanged(State, currentDefinition);

            if (definitionChanged)
            {
                State = currentDefinition;
                await WriteStateAsync();

                // CRITICAL: If definition changed, we MUST evict the pipeline cache BEFORE calling
                // HandlePipeline(), otherwise the old cached pipeline will be reused. The pipeline
                // cache key is (eventStore, namespace, projectionId), which does NOT include the
                // definition version, so a cache hit with the old definition will prevent the new
                // definition from being used.
                projectionPipelineManager.EvictFor(_key.EventStore, _key.Namespace, _key.ObserverId);
            }
        }

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
            logger.PipelineNotReady(_key);
            return new(
                ObserverSubscriberState.Failed,
                EventSequenceNumber.Unavailable,
                ["Projection pipeline is not yet ready — will be retried."],
                string.Empty);
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

                // Mirror the metadata fields that the storage sink writes so that watched models
                // match what a direct read from the underlying store returns.
                model[WellKnownProperties.LasHandledEventSequenceNumber] =
                    JsonValue.Create((ulong)lastSuccessfullyObservedEvent!.Context.SequenceNumber);

                var stateDict = (IDictionary<string, object?>)changeset.CurrentState;
                if (stateDict.TryGetValue(WellKnownProperties.Subject, out var subjectValue) && subjectValue is string subject)
                {
                    model[WellKnownProperties.Subject] = JsonValue.Create(subject);
                }

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
        _pipeline = await projectionPipelineManager.GetFor(_key.EventStore, _key.Namespace, projection);
        _schema = readModel.GetSchemaForLatestGeneration();
    }

    bool HasDefinitionChanged(ProjectionDefinition oldDefinition, ProjectionDefinition newDefinition)
    {
        if (oldDefinition.ReadModel != newDefinition.ReadModel ||
            oldDefinition.IsActive != newDefinition.IsActive ||
            oldDefinition.IsRewindable != newDefinition.IsRewindable ||
            oldDefinition.EventSequenceId != newDefinition.EventSequenceId)
        {
            return true;
        }

        if (oldDefinition.From.Count != newDefinition.From.Count ||
            oldDefinition.Join.Count != newDefinition.Join.Count ||
            oldDefinition.RemovedWith.Count != newDefinition.RemovedWith.Count ||
            oldDefinition.RemovedWithJoin.Count != newDefinition.RemovedWithJoin.Count)
        {
            return true;
        }

        foreach (var (eventType, oldFromRule) in oldDefinition.From)
        {
            if (!newDefinition.From.TryGetValue(eventType, out var newFromRule))
            {
                return true;
            }

            if (oldFromRule.Key != newFromRule.Key ||
                oldFromRule.Properties.Count != newFromRule.Properties.Count)
            {
                return true;
            }
        }

        foreach (var (eventType, oldJoinRule) in oldDefinition.Join)
        {
            if (!newDefinition.Join.TryGetValue(eventType, out var newJoinRule))
            {
                return true;
            }

            if (oldJoinRule.Key != newJoinRule.Key ||
                oldJoinRule.Properties.Count != newJoinRule.Properties.Count)
            {
                return true;
            }
        }

        return false;
    }
}
