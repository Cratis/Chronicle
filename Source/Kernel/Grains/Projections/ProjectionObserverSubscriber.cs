// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Engines.Projections;
using Aksio.Cratis.Kernel.Engines.Projections.Pipelines;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Projections;
using Aksio.DependencyInversion;

namespace Aksio.Cratis.Kernel.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionObserverSubscriber"/>.
/// </summary>
public class ProjectionObserverSubscriber : Grain, IProjectionObserverSubscriber, INotifyProjectionDefinitionsChanged
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IProjectionManager> _projectionManagerProvider;
    ProjectionId _projectionId;
    IProjectionPipeline? _pipeline;
    MicroserviceId _microserviceId = MicroserviceId.Unspecified;
    TenantId _tenantId = TenantId.NotSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionObserverSubscriber"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="projectionManagerProvider">Provider for <see cref="IProjectionManager"/> for working with projection instances.</param>
    public ProjectionObserverSubscriber(
        IExecutionContextManager executionContextManager,
        ProviderFor<IProjectionManager> projectionManagerProvider)
    {
        _executionContextManager = executionContextManager;
        _projectionManagerProvider = projectionManagerProvider;
        _projectionId = ProjectionId.NotSet;
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
            _executionContextManager.Establish(_tenantId, events.First().Context.CorrelationId, _microserviceId);
            foreach (var @event in events)
            {
                await _pipeline.Handle(@event);
                lastSuccessfullyObservedEvent = @event;
            }
            return ObserverSubscriberResult.Ok;
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
        _executionContextManager.Establish(_tenantId, CorrelationId.New(), _microserviceId);
        _pipeline = _projectionManagerProvider().GetPipeline(_projectionId);
    }
}
