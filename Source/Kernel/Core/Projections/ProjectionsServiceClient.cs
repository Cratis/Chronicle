// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Orleans.Runtime.Services;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionsServiceClient"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> to use for getting grains.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting services.</param>
public class ProjectionsServiceClient(IGrainFactory grainFactory, IServiceProvider serviceProvider) : GrainServiceClient<IProjectionsService>(serviceProvider), IProjectionsServiceClient
{
    readonly IManagementGrain _managementGrain = grainFactory.GetGrain<IManagementGrain>(1);

    /// <inheritdoc/>
    public async Task Register(EventStoreName eventStore, IEnumerable<ProjectionDefinition> definitions)
    {
        var hosts = await _managementGrain.GetHosts(true);
        foreach (var host in hosts.Keys)
        {
            await GetGrainService(host).Register(eventStore, definitions);
        }
    }

    /// <inheritdoc/>
    public async Task NamespaceAdded(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        var hosts = await _managementGrain.GetHosts(true);
        foreach (var host in hosts.Keys)
        {
            await GetGrainService(host).NamespaceAdded(eventStore, @namespace);
        }
    }
}
