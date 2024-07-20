// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Namespaces;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Definitions;
using Orleans.BroadcastChannel;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionsManager"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionsManager"/> class.
/// </remarks>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.ProjectionsManager)]
public class ProjectionsManager : Grain<ProjectionsManagerState>, IProjectionsManager, IOnBroadcastChannelSubscribed
{
    EventStoreName _eventStoreName = EventStoreName.NotSet;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _eventStoreName = this.GetPrimaryKeyString();
        await SetDefinitionForAllProjectionsInAllNamespaces();
    }

    /// <inheritdoc/>
    public async Task Register(IEnumerable<ProjectionDefinition> definitions)
    {
        State.Projections = definitions;
        await SetDefinitionForAllProjectionsInAllNamespaces();
    }

    /// <inheritdoc/>
    public Task OnSubscribed(IBroadcastChannelSubscription streamSubscription)
    {
        streamSubscription.Attach<NamespaceAdded>(OnNamespaceAdded, OnError);
        return Task.CompletedTask;
    }

    async Task OnNamespaceAdded(NamespaceAdded added)
    {
        foreach (var projectionDefinition in State.Projections)
        {
            var key = new ProjectionKey(projectionDefinition.Identifier, _eventStoreName, added.Namespace, projectionDefinition.EventSequenceId);
            var projection = GrainFactory.GetGrain<IProjection>(key);
            await projection.Ensure();
        }
    }

    async Task SetDefinitionForAllProjectionsInAllNamespaces()
    {
        var namespaces = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();
        foreach (var namespaceName in namespaces)
        {
            await SetDefinitionForAllProjectionsForNamespace(namespaceName);
        }
    }

    async Task SetDefinitionForAllProjectionsForNamespace(EventStoreNamespaceName namespaceName)
    {
        foreach (var projectionDefinition in State.Projections)
        {
            var key = new ProjectionKey(projectionDefinition.Identifier, _eventStoreName, namespaceName, projectionDefinition.EventSequenceId);
            var projection = GrainFactory.GetGrain<IProjection>(key);
            await projection.SetDefinition(projectionDefinition);
        }
    }

    Task OnError(Exception exception)
    {
        return Task.CompletedTask;
    }
}
