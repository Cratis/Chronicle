// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;

using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;
using EngineProjectionsManager = Cratis.Chronicle.Projections.Engine.IProjectionsManager;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IProjectionPartitionRebuilder"/>.
/// </summary>
/// <param name="engineProjectionsManager">The engine <see cref="EngineProjectionsManager"/> for the current projection.</param>
/// <param name="projectionPipelineManager">The <see cref="IProjectionPipelineManager"/> for creating the isolated pipeline.</param>
/// <param name="storage">The <see cref="IStorage"/> for reaching the live sink.</param>
/// <param name="admission">Revalidates admission before publishing a persisted replay decision.</param>
/// <param name="logger">The logger.</param>
public class ProjectionPartitionRebuilder(
    EngineProjectionsManager engineProjectionsManager,
    IProjectionPipelineManager projectionPipelineManager,
    IStorage storage,
    IPartitionRebuildAdmission admission,
    ILogger<ProjectionPartitionRebuilder> logger) : Grain, IProjectionPartitionRebuilder
{
    /// <summary>
    /// The reason given when events arrive before the rebuild has begun.
    /// </summary>
    internal const string NotBegun = "The isolated partition rebuild has not begun";

    readonly SemaphoreSlim _delivery = new(1, 1);
    Rebuild? _rebuild;

    /// <inheritdoc/>
    public async Task Begin(ObserverKey observerKey, Key partition)
    {
        _rebuild = null;
        var projection = await engineProjectionsManager.GetCurrent(observerKey.EventStore, observerKey.Namespace, observerKey.ObserverId)
            ?? throw new ProjectionPartitionRebuildHasNoProjection(observerKey);
        var sink = new PartitionRebuildSink(partition);
        var pipeline = projectionPipelineManager.CreateIsolated(observerKey.EventStore, observerKey.Namespace, projection, sink);
        _rebuild = new(observerKey, partition, projection, pipeline, sink);
        logger.BeganIsolatedRebuild(observerKey.ObserverId, partition);
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        if (_rebuild is null)
        {
            return ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, NotBegun);
        }

        if (!Equals(partition.Value, _rebuild.Partition.Value))
        {
            return ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, $"The isolated rebuild of partition '{_rebuild.Partition}' was given events for partition '{partition}'");
        }

        // A timeout abandons the acknowledgement, not necessarily the delivery. Serialize redelivery with
        // the abandoned call, then skip its completed prefix. Begin starts a new, empty attempt.
        await _delivery.WaitAsync();
        try
        {
            return await HandleDelivery(events);
        }
        finally
        {
            _delivery.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> Publish(EventSequenceNumber watermark)
    {
        var rebuild = _rebuild ?? throw new InvalidOperationException(NotBegun);
        if (!watermark.IsActualValue || rebuild.Failed)
        {
            return false;
        }

        var key = rebuild.ObserverKey;
        var liveSink = await storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).Sinks.GetFor(rebuild.Projection.ReadModel);
        var request = new ReplayObserverPartitionRequest(key, ObserverType.Projection, rebuild.Partition, EventSequenceNumber.First, watermark, rebuild.Projection.EventTypes)
        {
            ReplaysAllEventTypes = true
        };
        // The persisted prepare-time decision is not authority to write after generation initialization.
        // Check after resolving the live sink as well, since resolution itself can await storage.
        if (!await admission.IsAdmitted(request))
        {
            rebuild.Failed = true;
            return false;
        }

        var result = await liveSink.ReplaceIsolated(rebuild.Partition, rebuild.Sink.State, watermark);
        _rebuild = null;
        DeactivateOnIdle();
        logger.PublishedIsolatedRebuild(key.ObserverId, rebuild.Partition, result);
        return result != IsolatedReplaceResult.Superseded;
    }

    async Task<ObserverSubscriberResult> HandleDelivery(IEnumerable<AppendedEvent> events)
    {
        var rebuild = _rebuild!;
        if (rebuild.Failed)
        {
            return ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "The isolated rebuild failed; begin a new attempt before redelivery");
        }

        try
        {
            foreach (var @event in events)
            {
                if (rebuild.LastApplied.IsActualValue && @event.Context.SequenceNumber <= rebuild.LastApplied) continue;
                if (!await Handle(@event))
                {
                    rebuild.Failed = true;
                    return ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "The isolated rebuild failed while applying an event");
                }

                rebuild.LastApplied = @event.Context.SequenceNumber;
            }

            return ObserverSubscriberResult.Ok(rebuild.LastApplied);
        }
        catch (Exception exception)
        {
            // The pipeline may have mutated the accumulator before throwing. Never retry that event into
            // this accumulator or publish it; the next step attempt must Begin and fold from empty.
            rebuild.Failed = true;
            logger.FailedIsolatedRebuild(exception, rebuild.ObserverKey.ObserverId, rebuild.Partition);
            return new(ObserverSubscriberState.Failed, EventSequenceNumber.Unavailable, exception.GetAllMessages(), exception.StackTrace ?? string.Empty);
        }
    }

    async Task<bool> Handle(AppendedEvent @event)
    {
        var projection = _rebuild!.Projection;
        var pin = projection.GenerationDelivery == EventGenerationDelivery.Pinned
            ? projection.EventTypes.FirstOrDefault(_ => _.Id == @event.Context.EventType.Id)
            : null;
        if (pin is null && !projection.Accepts(@event.Context.EventType))
        {
            return true;
        }

        var eventToHandle = pin is not null && pin != @event.Context.EventType
            ? await projection.ReleaseEvent(pin, @event)
            : @event;
        var context = await _rebuild.Pipeline.Handle(eventToHandle);
        return !context.FailedPartitions.Any();
    }

    sealed record Rebuild(ObserverKey ObserverKey, Key Partition, EngineProjection Projection, IProjectionPipeline Pipeline, PartitionRebuildSink Sink)
    {
        public EventSequenceNumber LastApplied { get; set; } = EventSequenceNumber.Unavailable;
        public bool Failed { get; set; }
    }
}
