// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Grains.Observation;
using Cratis.Kernel.Orleans.Observers;
using Cratis.Observation;
using Cratis.Projections;
using Cratis.Projections.Definitions;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using EngineProjection = Cratis.Kernel.Projections.IProjection;

namespace Cratis.Kernel.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjection"/>.
/// </summary>
public class Projection : Grain, IProjection
{
    readonly ObserverManager<INotifyProjectionDefinitionsChanged> _definitionObservers;
    readonly IKernel _kernel;
    readonly ILocalSiloDetails _localSiloDetails;
    EngineProjection? _projection;
    IObserver? _observer;
    ProjectionId _projectionId;
    ProjectionDefinition? _definition;
    TenantId? _tenantId;
    MicroserviceId? _microserviceId;

    /// <summary>
    /// Initializes a new instance of the <see cref="Projection"/> class.
    /// </summary>
    /// <param name="kernel"><see cref="IKernel"/> for accessing global artifacts.</param>
    /// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for getting information about the silo this grain is on.</param>
    /// <param name="logger">Logger for logging.</param>
    public Projection(
        IKernel kernel,
        ILocalSiloDetails localSiloDetails,
        ILogger<Projection> logger)
    {
        _projectionId = ProjectionId.NotSet;
        _definitionObservers = new(TimeSpan.FromMinutes(1), logger, "ProjectionDefinitionObservers");
        _kernel = kernel;
        _localSiloDetails = localSiloDetails;
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _projectionId = this.GetPrimaryKey(out var keyAsString);
        var key = ProjectionKey.Parse(keyAsString);
        _microserviceId = key.MicroserviceId;
        _tenantId = key.TenantId;

        await RefreshDefinition();

        _observer = GrainFactory.GetGrain<IObserver>(_projectionId, new ObserverKey(key.MicroserviceId, key.TenantId, key.EventSequenceId));

        await _observer.Subscribe<IProjectionObserverSubscriber>(
            _definition!.Name.Value,
            ObserverType.Projection,
            _projection!.EventTypes,
            _localSiloDetails.SiloAddress);
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
        var eventStore = _kernel.GetEventStore((string)_microserviceId!);
        var eventStoreNamespace = eventStore.GetNamespace(_tenantId!);

        var (_, projectionDefinition) = await eventStore.ProjectionDefinitions.TryGetFor(_projectionId);
        _definition = projectionDefinition;
        _projection = eventStoreNamespace.ProjectionManager.Get(_definition!.Identifier);
        _definitionObservers.Notify(_ => _.OnProjectionDefinitionsChanged());
    }

    /// <inheritdoc/>
    public Task Ensure() => Task.CompletedTask;
}
