// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.ObserverRouting;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Extension methods for wiring up the silo-local observer routing table and its revision protocol.
/// </summary>
public static class ObserverRoutingExtensions
{
    /// <summary>
    /// Add the per-silo grain service that acknowledges definition revisions, and the client that publishes them.
    /// </summary>
    /// <param name="siloBuilder"><see cref="ISiloBuilder"/> to configure for.</param>
    /// <returns><see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder AddObserverRouting(this ISiloBuilder siloBuilder)
    {
        siloBuilder.AddGrainService<ObserverRoutingGrainService>();
        siloBuilder.ConfigureServices(_ => _.AddSingleton<IObserverRoutingClient, ObserverRoutingClient>());
        return siloBuilder;
    }
}
