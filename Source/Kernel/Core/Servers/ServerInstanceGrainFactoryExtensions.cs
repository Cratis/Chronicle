// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Servers;

/// <summary>
/// Extension methods for <see cref="IGrainFactory"/> for working with <see cref="IServerInstance"/>.
/// </summary>
public static class ServerInstanceGrainFactoryExtensions
{
    /// <summary>
    /// Gets the <see cref="IServerInstance"/> grain reporting live resource usage for a specific silo.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> to get the grain from.</param>
    /// <param name="siloAddress">The <see cref="SiloAddress"/> of the silo to get metrics for.</param>
    /// <returns>The <see cref="IServerInstance"/> grain for the silo.</returns>
    public static IServerInstance GetServerInstance(this IGrainFactory grainFactory, SiloAddress siloAddress) =>
        grainFactory.GetGrain<IServerInstance>(siloAddress.ToParsableString());
}
