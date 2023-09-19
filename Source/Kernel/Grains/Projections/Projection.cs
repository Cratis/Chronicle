// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Engines.Projections;
using Aksio.Cratis.Kernel.Engines.Projections.Definitions;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Kernel.Orleans.Observers;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Projections.Definitions;
using Aksio.DependencyInversion;
using Microsoft.Extensions.Logging;
using EngineProjection = Aksio.Cratis.Kernel.Engines.Projections.IProjection;

namespace Aksio.Cratis.Kernel.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjection"/>.
/// </summary>
public class Projection : Grain, IProjection
{
    readonly ProviderFor<IProjectionDefinitions> _projectionDefinitionsProvider;
    readonly ProviderFor<IProjectionManager> _projectionManagerProvider;
    readonly IExecutionContextManager _executionContextManager;
    readonly ObserverManager<INotifyProjectionDefinitionsChanged> _definitionObservers;
    EngineProjection? _projection;
    IObserverSupervisor? _observer;
    ProjectionId _projectionId;
    ProjectionDefinition? _definition;
    TenantId? _tenantId;
    MicroserviceId? _microserviceId;

    /// <summary>
    /// Initializes a new instance of the <see cref="Projection"/> class.
    /// </summary>
    /// <param name="projectionDefinitionsProvider"><see cref="IProjectionDefinitions"/>.</param>
    /// <param name="projectionManagerProvider"><see cref="IProjectionManager"/> for working with engine projections.</param>
    /// <param name="executionContextManager">The <see cref="IExecutionContextManager"/>.</param>
    /// <param name="logger">Logger for logging.</param>
    public Projection(
        ProviderFor<IProjectionDefinitions> projectionDefinitionsProvider,
        ProviderFor<IProjectionManager> projectionManagerProvider,
        IExecutionContextManager executionContextManager,
        ILogger<Projection> logger)
    {
        _projectionDefinitionsProvider = projectionDefinitionsProvider;
        _projectionManagerProvider = projectionManagerProvider;
        _executionContextManager = executionContextManager;
        _projectionId = ProjectionId.NotSet;
        _definitionObservers = new(TimeSpan.FromMinutes(1), logger, "ProjectionDefinitionObservers");
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _projectionId = this.GetPrimaryKey(out var keyAsString);
        var key = ProjectionKey.Parse(keyAsString);
        _microserviceId = key.MicroserviceId;
        _tenantId = key.TenantId;

        // TODO: This is a temporary work-around till we fix #264 & #265
        _executionContextManager.Establish(key.TenantId, CorrelationId.New(), key.MicroserviceId);

        await RefreshDefinition();

        _observer = GrainFactory.GetGrain<IObserverSupervisor>(_projectionId, new ObserverKey(key.MicroserviceId, key.TenantId, key.EventSequenceId));

        await _observer.SetNameAndType(_definition!.Name.Value, ObserverType.Projection);
        await _observer.Subscribe<IProjectionObserverSubscriber>(_projection!.EventTypes);
    }

    /// <inheritdoc/>
    public Task SubscribeDefinitionsChanged(INotifyProjectionDefinitionsChanged subscriber)
    {
        _definitionObservers.Subscribe(subscriber, subscriber);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task RefreshDefinition()
    {
        // TODO: This is a temporary work-around till we fix #264 & #265
        _executionContextManager.Establish(_tenantId!, CorrelationId.New(), _microserviceId);

        _definition = await _projectionDefinitionsProvider().GetFor(_projectionId);
        _projection = _projectionManagerProvider().Get(_definition!.Identifier);
        _definitionObservers.Notify(_ => _.OnProjectionDefinitionsChanged());
    }

    /// <inheritdoc/>
    public Task Ensure() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task Rewind()
    {
        _observer?.Replay();
        return Task.CompletedTask;
    }
}
