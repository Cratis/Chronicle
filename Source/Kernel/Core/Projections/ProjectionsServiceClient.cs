// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Monads;
using Orleans.Runtime.Services;
using Polly;
using Polly.Registry;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionsServiceClient"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> to use for getting grains.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting services.</param>
/// <param name="resiliencePipelineProvider">The <see cref="ResiliencePipelineProvider{TKey}"/>.</param>
public class ProjectionsServiceClient(IGrainFactory grainFactory, IServiceProvider serviceProvider, ResiliencePipelineProvider<string> resiliencePipelineProvider) : GrainServiceClient<IProjectionsService>(serviceProvider), IProjectionsServiceClient
{
    /// <summary>
    /// The <see cref="ResiliencePipeline"/> key.
    /// </summary>
    public const string ResiliencePipelineKey = "Retry-Projections-Service";

    readonly IManagementGrain _managementGrain = grainFactory.GetGrain<IManagementGrain>(1);
    readonly ResiliencePipeline _resiliencePipeline = resiliencePipelineProvider.GetPipeline(ResiliencePipelineKey);

    /// <inheritdoc/>
    public async Task<Result<ProjectionRegistrationError>> Register(EventStoreName eventStore, IEnumerable<ProjectionDefinition> definitions)
    {
        var definitionList = definitions.ToList();
        var hosts = await _managementGrain.GetHosts(true);
        var results = await Task.WhenAll(hosts.Keys.Select(host => InvokeResilient(host, service => service.Register(eventStore, definitionList))));
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
        await Task.WhenAll(hosts.Keys.Select(host => InvokeResilient(host, callback)));
    }

    Task<TResult> InvokeResilient<TResult>(SiloAddress host, Func<IProjectionsService, Task<TResult>> callback) =>
        _resiliencePipeline.ExecuteAsync(async _ => await callback(GetGrainService(host))).AsTask();

    Task InvokeResilient(SiloAddress host, Func<IProjectionsService, Task> callback) =>
        _resiliencePipeline.ExecuteAsync(async _ => await callback(GetGrainService(host))).AsTask();
}
