// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionObserverSubscriber"/> and
/// <see cref="ICollapsingProjectionObserverSubscriber"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionObserverSubscriber"/> class.
/// <para>
/// Which of the two interfaces a projection subscribes as decides how its activations are keyed, and that is the
/// whole difference between them: a projection keyed on the event source id gets an activation per partition,
/// spread across the cluster, while a projection whose key can collapse several event sources onto one document
/// gets a single activation for every partition. The latter is what makes the coarse mode of
/// <see cref="ProjectionHandleLock"/> - which is process local - sufficient: without it, two silos would run the
/// read-modify-write cycle against the same document without seeing each other's lock. The cost is that a
/// collapsing projection is bounded by one activation on one silo, which is what the coarse lock already implied
/// within a silo.
/// </para>
/// </remarks>
/// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating projections.</param>
/// <param name="projectionPipelineManager"><see cref="IProjectionPipelineManager"/> for creating projection pipelines.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting to and from <see cref="ExpandoObject"/>.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing storage.</param>
/// <param name="logger">The logger.</param>
public class ProjectionObserverSubscriber(
    IProjectionFactory projectionFactory,
    IProjectionPipelineManager projectionPipelineManager,
    IExpandoObjectConverter expandoObjectConverter,
    IStorage storage,
    ILogger<ProjectionObserverSubscriber> logger) : Grain, IProjectionObserverSubscriber, ICollapsingProjectionObserverSubscriber, INotifyProjectionDefinitionsChanged
{
    ObserverSubscriberKey _key = ObserverSubscriberKey.Unspecified;
    IProjectionPipeline? _pipeline;
    EngineProjection? _projection;
    IProjectionChangesetNotifier? _changesetNotifier;
    JsonSchema? _schema;
    Exception? _pipelineBuildFailure;
    ProjectionDefinition _definition = null!;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        (_, _key) = this.GetKeys();

        // Direct grain-to-observer notification — the previous Orleans MemoryStreams pub-sub
        // propagated subscribers to the producer's PullingAgent on a periodic refresh that
        // could lag the first publish and silently drop it under load.
        _changesetNotifier = GrainFactory.GetGrain<IProjectionChangesetNotifier>(_key.ObserverId);

        var projection = GrainFactory.GetGrain<IProjection>(new ProjectionKey(_key.ObserverId, _key.EventStore));
        await projection.SubscribeDefinitionsChanged(this.AsReference<INotifyProjectionDefinitionsChanged>());

        // Only the Projection grain persists definitions. Subscribers retain no grain state.
        _definition = await projection.GetDefinition();
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
        _definition = definition;

        // Pipeline caches are silo-local; a definition notification must evict this subscriber's cache too.
        projectionPipelineManager.EvictFor(_key.EventStore, _key.Namespace, _key.ObserverId);
        await HandlePipeline();
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        var (result, notification) = await HandleRun(partition, events);
        if (result.State != ObserverSubscriberState.Ok)
        {
            return result;
        }

        try
        {
            if (notification is not null)
            {
                await _changesetNotifier!.Notify(_key.Namespace, notification.ReadModelKey, notification.ReadModel, notification.Change);
            }

            logger.SuccessfullyHandledAllEvents(_key);
            return result;
        }
        catch (Exception ex)
        {
            logger.ErrorHandling(ex, _key, result.LastSuccessfulObservation);
            return new(ObserverSubscriberState.Failed, result.LastSuccessfulObservation, ex.GetAllMessages(), ex.StackTrace ?? string.Empty);
        }
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNextPage(IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        var page = events.ToArray();
        var notifications = new List<ReadModelChangeNotification>();
        var lastSuccessful = EventSequenceNumber.Unavailable;
        ObserverSubscriberResult? failure = null;
        Key? lastHandledPartition = null;
        await PreparePage(page);
        var offset = 0;
        while (offset < page.Length)
        {
            var start = offset;
            var partition = page[start].Context.EventSourceId;
            while (offset < page.Length && page[offset].Context.EventSourceId == partition)
            {
                offset++;
            }

            var run = page[start..offset];
            var (result, notification) = await HandleRun(partition, run);
            if (result.State != ObserverSubscriberState.Ok)
            {
                var failedEvent = result.HandledAnyEvents
                    ? run.FirstOrDefault(_ => _.Context.SequenceNumber > result.LastSuccessfulObservation) ?? run[^1]
                    : run[0];
                failure = result with
                {
                    LastSuccessfulObservation = result.HandledAnyEvents ? result.LastSuccessfulObservation : lastSuccessful,
                    FailedPartition = partition,
                    FailedAt = failedEvent.Context.SequenceNumber
                };
                break;
            }

            if (notification is not null)
            {
                notifications.Add(notification);
            }

            logger.SuccessfullyHandledAllEvents(_key);
            lastSuccessful = result.LastSuccessfulObservation;
            lastHandledPartition = partition;
        }

        if (notifications.Count > 0)
        {
            try
            {
                await _changesetNotifier!.NotifyMany(_key.Namespace, notifications);
            }
            catch (Exception ex)
            {
                // The handled runs are already persisted, so the position stays at the last handled event, as a failed
                // Notify did for a single run. A run that failed on its own keeps its own failure.
                logger.ErrorHandling(ex, _key, lastSuccessful);
                failure ??= new ObserverSubscriberResult(ObserverSubscriberState.Failed, lastSuccessful, ex.GetAllMessages(), ex.StackTrace ?? string.Empty)
                {
                    FailedPartition = lastHandledPartition,
                    FailedAt = lastSuccessful
                };
            }
        }

        return failure ?? ObserverSubscriberResult.Ok(lastSuccessful);
    }

    async Task PreparePage(AppendedEvent[] page)
    {
        if (page.Length == 0)
        {
            return;
        }

        if (_pipeline is null)
        {
            await HandlePipeline();
        }

        if (_pipeline is null)
        {
            return;
        }

        try
        {
            await _pipeline.PreparePage(page);
        }
        catch (Exception ex)
        {
            logger.FailedPreparingPage(ex, _key);
        }
    }

    async Task<(ObserverSubscriberResult Result, ReadModelChangeNotification? Notification)> HandleRun(Key partition, IEnumerable<AppendedEvent> events)
    {
        if (_pipeline is null)
        {
            // Retry the build on every delivery: a pipeline that failed to build (e.g. from a stale definition)
            // heals as soon as the definition is corrected, without requiring the grain to be reactivated.
            await HandlePipeline();
        }

        if (_pipeline is null)
        {
            if (_pipelineBuildFailure is not null)
            {
                return (new(
                    ObserverSubscriberState.Failed,
                    EventSequenceNumber.Unavailable,
                    _pipelineBuildFailure.GetAllMessages(),
                    _pipelineBuildFailure.StackTrace ?? string.Empty), null);
            }

            logger.PipelineNotReady(_key);
            return (new(
                ObserverSubscriberState.Failed,
                EventSequenceNumber.Unavailable,
                ["Projection pipeline is not yet ready — will be retried."],
                string.Empty), null);
        }

        AppendedEvent? lastSuccessfullyObservedEvent = default;
        try
        {
            ReadModelChangeNotification? notification = null;
            IChangeset<AppendedEvent, ExpandoObject>? changeset = null;
            var isNewInstance = false;

            foreach (var @event in events)
            {
                var sequenceNumber = @event.Context.SequenceNumber;

                // Definition notifications precede the subscription rebind, so a handled type can still arrive
                // in the old pin. Only an unhandled type may be skipped; a generation mismatch must be processed.
                if (_projection?.Accepts(@event.Context.EventType) == false)
                {
                    // SetDefinition waits for this subscriber's notification. Calling that grain here
                    // could deadlock, so re-check shared storage directly before deciding to skip.
                    var definitions = storage.GetEventStore(_key.EventStore).Projections;
                    if (!await definitions.Has(_key.ObserverId))
                    {
                        logger.PipelineNotReady(_key);
                        return (new(
                            ObserverSubscriberState.Failed,
                            lastSuccessfullyObservedEvent?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable,
                            ["Projection definition no longer exists."],
                            string.Empty), null);
                    }

                    var current = await definitions.Get(_key.ObserverId);
                    if (ProjectionFactory.DefinitionFingerprintFor(_definition) != ProjectionFactory.DefinitionFingerprintFor(current))
                    {
                        _definition = current;
                        projectionPipelineManager.EvictFor(_key.EventStore, _key.Namespace, _key.ObserverId);
                        await HandlePipeline();
                        if (_pipeline is null)
                        {
                            return (new(
                                ObserverSubscriberState.Failed,
                                lastSuccessfullyObservedEvent?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable,
                                _pipelineBuildFailure?.GetAllMessages() ?? ["Projection pipeline is not yet ready — will be retried."],
                                _pipelineBuildFailure?.StackTrace ?? string.Empty), null);
                        }
                    }
                }

                var pin = _projection?.GenerationDelivery == EventGenerationDelivery.Pinned
                    ? _projection.EventTypes.FirstOrDefault(_ => _.Id == @event.Context.EventType.Id)
                    : null;
                if (pin is null && _projection?.Accepts(@event.Context.EventType) == false)
                {
                    lastSuccessfullyObservedEvent = @event;
                    logger.SkippedEventNotInProjection(@event.Context.EventType, sequenceNumber, _key);
                    continue;
                }

                // Already-pinned events have been released by the observer. Re-select/release only a mismatch
                // through the current projection's release boundary, which also migrates an unstored generation.
                var eventToHandle = pin is not null && pin != @event.Context.EventType
                    ? await _projection!.ReleaseEvent(pin, @event)
                    : @event;
                var pipelineContext = await _pipeline.Handle(eventToHandle);
                changeset = pipelineContext.Changeset;

                // Accumulate across the batch: a single OnNext delivers all events for one partition
                // (e.g. AppendMany, or an observer draining a backlog on catch-up). If the instance was
                // created by any event in the batch, the net change is an Add even when a later event in
                // the same batch updates it — so OR rather than overwrite, else the create is misreported
                // as Modified. HasBeenRemoved() on the final changeset still takes precedence below.
                isNewInstance |= pipelineContext.IsNewInstance;

                // Check if there are any failed partitions from bulk operations
                if (pipelineContext.FailedPartitions.Any())
                {
                    var observer = GrainFactory.GetGrain<IObserver>(new ObserverKey(_key.ObserverId, _key.EventStore, _key.Namespace, _key.EventSequenceId));
                    foreach (var failedPartition in pipelineContext.FailedPartitions)
                    {
                        await observer.PartitionFailed(
                            failedPartition.EventSourceId,
                            failedPartition.EventSequenceNumber,
                            [ProjectionBulkFailures.MessageFor(failedPartition)],
                            string.Empty,
                            FailureKind.Handling);
                    }
                }

                lastSuccessfullyObservedEvent = @event;
                logger.SuccessfullyHandledEvent(sequenceNumber, _key);
            }

            // Note: We don't want to send changesets if the projection is not active
            if (changeset?.HasChanges == true && _definition.IsActive)
            {
                var model = expandoObjectConverter.ToJsonObject(changeset.CurrentState, _schema!);

                // Mirror the metadata fields that the storage sink writes so that watched models
                // match what a direct read from the underlying store returns.
                model[WellKnownProperties.LastHandledEventSequenceNumber] =
                    JsonValue.Create((ulong)lastSuccessfullyObservedEvent!.Context.SequenceNumber);

                var stateDict = (IDictionary<string, object?>)changeset.CurrentState;
                if (stateDict.TryGetValue(WellKnownProperties.Subject, out var subjectValue) && subjectValue is string subject)
                {
                    model[WellKnownProperties.Subject] = JsonValue.Create(subject);
                }

                if (stateDict.TryGetValue(WellKnownProperties.Subjects, out var subjectsValue))
                {
                    model[WellKnownProperties.Subjects] = JsonSerializer.SerializeToNode(subjectsValue);
                }

                var changeType = (changeset.HasBeenRemoved(), isNewInstance) switch
                {
                    (true, _) => ReadModelChangeType.Removed,
                    (false, true) => ReadModelChangeType.Added,
                    _ => ReadModelChangeType.Modified
                };

                var change = new ReadModelChangeContext(
                    changeType,
                    lastSuccessfullyObservedEvent!.Context.SequenceNumber,
                    lastSuccessfullyObservedEvent.Context.Occurred,
                    lastSuccessfullyObservedEvent.Context.CorrelationId);

                notification = new(partition.ToString(), model, change);
            }

            return (ObserverSubscriberResult.Ok(lastSuccessfullyObservedEvent!.Context.SequenceNumber), notification);
        }
        catch (Exception ex)
        {
            logger.ErrorHandling(ex, _key, lastSuccessfullyObservedEvent?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable);
            return (new(
                ObserverSubscriberState.Failed,
                lastSuccessfullyObservedEvent?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable,
                ex.GetAllMessages(),
                ex.StackTrace ?? string.Empty), null);
        }
    }

    async Task HandlePipeline()
    {
        if (_definition.ReadModel is null)
        {
            return;
        }

        // A build failure must not fault the grain: throwing from OnActivateAsync would fail the activation
        // itself and every partition would report the activation error over and over. Containing the failure
        // here keeps the grain alive, lets OnNext report the actual cause on the failed partition, and lets a
        // later definition change (or the retry in OnNext) rebuild the pipeline without a reactivation.
        try
        {
            var readModel = await GrainFactory.GetGrain<IReadModel>(new ReadModelGrainKey(_definition.ReadModel, _key.EventStore)).GetDefinition();
            var eventStoreStorage = storage.GetEventStore(_key.EventStore);
            var eventTypeSchemas = await eventStoreStorage.EventTypes.GetLatestForAllEventTypes();
            var projection = await projectionFactory.Create(_key.EventStore, _key.Namespace, _definition, readModel, eventTypeSchemas);
            _pipeline = await projectionPipelineManager.GetFor(_key.EventStore, _key.Namespace, projection);
            _projection = projection;
            _schema = readModel.GetSchemaForLatestGeneration();
            _pipelineBuildFailure = null;
        }
        catch (Exception exception)
        {
            _pipeline = null;
            _projection = null;
            _pipelineBuildFailure = exception;
            logger.FailedBuildingPipeline(exception, _key);
        }
    }
}
