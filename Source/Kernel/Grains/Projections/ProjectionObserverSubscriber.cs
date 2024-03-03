// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Kernel.Projections.Pipelines;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Projections;

namespace Aksio.Cratis.Kernel.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionObserverSubscriber"/>.
/// </summary>
public class ProjectionObserverSubscriber : Grain, IProjectionObserverSubscriber, INotifyProjectionDefinitionsChanged
{
    readonly IKernel _kernel;
    ProjectionId _projectionId;
    IProjectionPipeline? _pipeline;
    MicroserviceId _microserviceId = MicroserviceId.Unspecified;
    TenantId _tenantId = TenantId.NotSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionObserverSubscriber"/> class.
    /// </summary>
    /// <param name="kernel"><see cref="IKernel"/> for accessing global artifacts.</param>
    public ProjectionObserverSubscriber(IKernel kernel)
    {
        _projectionId = ProjectionId.NotSet;
        _kernel = kernel;
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _projectionId = this.GetPrimaryKey(out var keyAsString);
        var key = ObserverSubscriberKey.Parse(keyAsString);
        _microserviceId = key.MicroserviceId;
        _tenantId = key.TenantId;
        var projection = GrainFactory.GetGrain<IProjection>(_projectionId, new ProjectionKey(key.MicroserviceId, key.TenantId, key.EventSequenceId));
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
        var projectionManager = _kernel.GetEventStore((string)_microserviceId).GetNamespace(_tenantId).ProjectionManager;
        _pipeline = projectionManager.GetPipeline(_projectionId);
    }
}
