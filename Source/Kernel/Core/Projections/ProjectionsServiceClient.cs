// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Monads;
using Orleans.Runtime.Services;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionsServiceClient"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> to use for getting grains.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting services.</param>
public class ProjectionsServiceClient(IGrainFactory grainFactory, IServiceProvider serviceProvider) : GrainServiceClient<IProjectionsService>(serviceProvider), IProjectionsServiceClient
{
    readonly IManagementGrain _managementGrain = grainFactory.GetGrain<IManagementGrain>(1);

    /// <inheritdoc/>
    public async Task<Result<ProjectionRegistrationError>> Register(EventStoreName eventStore, IEnumerable<ProjectionDefinition> definitions)
    {
        var definitionList = definitions.ToList();
        var hosts = await _managementGrain.GetHosts(true);
        var results = await Task.WhenAll(hosts.Keys.Select(host => GetGrainService(host).Register(eventStore, definitionList)));
        var failures = new Dictionary<ProjectionId, Exception>();
        foreach (var result in results)
        {
            if (result.TryGetError(out var error))
            {
                foreach (var (identifier, failure) in error.Failures)
                {
                    failures.TryAdd(identifier, failure);
                }
            }
        }

        return failures.Count == 0
            ? Result<ProjectionRegistrationError>.Success()
            : Result.Failed(new ProjectionRegistrationError(failures));
    }

    /// <inheritdoc/>
    public async Task Unregister(EventStoreName eventStore, ProjectionId projectionId) =>
        await ForEachGrainService(service => service.Unregister(eventStore, projectionId));

    /// <inheritdoc/>
    public async Task NamespaceAdded(EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        await ForEachGrainService(service => service.NamespaceAdded(eventStore, @namespace));

    async Task ForEachGrainService(Func<IProjectionsService, Task> callback)
    {
        var hosts = await _managementGrain.GetHosts(true);
        await Task.WhenAll(hosts.Keys.Select(host => callback(GetGrainService(host))));
    }
}
