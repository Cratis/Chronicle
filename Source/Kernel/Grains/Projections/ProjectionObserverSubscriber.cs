// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.Kernel.Grains.Observation;
using Cratis.Kernel.Projections.Pipelines;
using Cratis.Observation;
using Cratis.Projections;

namespace Cratis.Kernel.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionObserverSubscriber"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionObserverSubscriber"/> class.
/// </remarks>
/// <param name="kernel"><see cref="IKernel"/> for accessing global artifacts.</param>
public class ProjectionObserverSubscriber(IKernel kernel) : Grain, IProjectionObserverSubscriber, INotifyProjectionDefinitionsChanged
{
    ProjectionId _projectionId = ProjectionId.NotSet;
    IProjectionPipeline? _pipeline;
    EventStoreName _eventStore = EventStoreName.NotSet;
    EventStoreNamespaceName _namespace = EventStoreNamespaceName.NotSet;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _projectionId = this.GetPrimaryKey(out var keyAsString);
        var key = ObserverSubscriberKey.Parse(keyAsString);
        _eventStore = key.EventStore;
        _namespace = key.Namespace;
        var projection = GrainFactory.GetGrain<IProjection>(_projectionId, new ProjectionKey(key.EventStore, key.Namespace, key.EventSequenceId));
        await projection.SubscribeDefinitionsChanged(this.AsReference<INotifyProjectionDefinitionsChanged>());

        HandleDefinitionsAndInstances();
    }

    /// <inheritdoc/>
    public void OnProjectionDefinitionsChanged()
    {
        HandleDefinitionsAndInstances();
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        if (_pipeline is null)
        {
            return ObserverSubscriberResult.Disconnected(EventSequenceNumber.Unavailable);
        }

        AppendedEvent? lastSuccessfullyObservedEvent = default;

        try
        {
            foreach (var @event in events)
            {
                await _pipeline.Handle(@event);
                lastSuccessfullyObservedEvent = @event;
            }
            return ObserverSubscriberResult.Ok(events.Last().Metadata.SequenceNumber);
        }
        catch (Exception ex)
        {
            return new(
                ObserverSubscriberState.Failed,
                lastSuccessfullyObservedEvent?.Metadata.SequenceNumber ?? EventSequenceNumber.Unavailable,
                ex.GetAllMessages(),
                ex.StackTrace ?? string.Empty);
        }
    }

    void HandleDefinitionsAndInstances()
    {
        var projectionManager = kernel.GetEventStore((string)_eventStore).GetNamespace(_namespace).ProjectionManager;
        _pipeline = projectionManager.GetPipeline(_projectionId);
    }
}
