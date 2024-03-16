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
/// <remarks>
/// Initializes a new instance of the <see cref="Projection"/> class.
/// </remarks>
/// <param name="kernel"><see cref="IKernel"/> for accessing global artifacts.</param>
/// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for getting information about the silo this grain is on.</param>
/// <param name="logger">Logger for logging.</param>
public class Projection(
    IKernel kernel,
    ILocalSiloDetails localSiloDetails,
    ILogger<Projection> logger) : Grain, IProjection
{
    readonly ObserverManager<INotifyProjectionDefinitionsChanged> _definitionObservers = new(TimeSpan.FromMinutes(1), logger, "ProjectionDefinitionObservers");
    EngineProjection? _projection;
    IObserver? _observer;
    ProjectionId _projectionId = ProjectionId.NotSet;
    ProjectionDefinition? _definition;
    TenantId? _tenantId;
    MicroserviceId? _microserviceId;

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
            localSiloDetails.SiloAddress);
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
        var eventStore = kernel.GetEventStore((string)_microserviceId!);
        var eventStoreNamespace = eventStore.GetNamespace(_tenantId!);

        var (_, projectionDefinition) = await eventStore.ProjectionDefinitions.TryGetFor(_projectionId);
        _definition = projectionDefinition;
        _projection = eventStoreNamespace.ProjectionManager.Get(_definition!.Identifier);
        _definitionObservers.Notify(_ => _.OnProjectionDefinitionsChanged());
    }

    /// <inheritdoc/>
    public Task Ensure() => Task.CompletedTask;
}
