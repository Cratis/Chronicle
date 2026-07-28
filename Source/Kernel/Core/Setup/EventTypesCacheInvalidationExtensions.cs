// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventTypes;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable SA1600
namespace Cratis.Chronicle.Setup;

/// <summary>
/// Extension methods for wiring up cluster-wide event type cache invalidation.
/// </summary>
public static class EventTypesCacheInvalidationExtensions
{
    /// <summary>
    /// Add the per-silo grain service and its fan-out client used to evict event type caches across the cluster.
    /// </summary>
    /// <param name="siloBuilder"><see cref="ISiloBuilder"/> to configure for.</param>
    /// <returns><see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder AddEventTypesCacheInvalidation(this ISiloBuilder siloBuilder)
    {
        siloBuilder.AddGrainService<EventTypesCacheGrainService>();
        siloBuilder.ConfigureServices(_ => _.AddSingleton<IEventTypesCacheClient, EventTypesCacheClient>());
        return siloBuilder;
    }
}
