// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Extension methods for wiring up cluster-wide observer definition registry invalidation.
/// </summary>
public static class ObserverDefinitionsCacheInvalidationExtensions
{
    /// <summary>
    /// Add the per-silo grain service and its fan-out client used to refresh observer definition registries across the cluster.
    /// </summary>
    /// <param name="siloBuilder"><see cref="ISiloBuilder"/> to configure for.</param>
    /// <returns><see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder AddObserverDefinitionsCacheInvalidation(this ISiloBuilder siloBuilder)
    {
        siloBuilder.AddGrainService<ObserverDefinitionsCacheGrainService>();
        siloBuilder.ConfigureServices(_ => _.AddSingleton<IObserverDefinitionsCacheClient, ObserverDefinitionsCacheClient>());
        return siloBuilder;
    }
}
