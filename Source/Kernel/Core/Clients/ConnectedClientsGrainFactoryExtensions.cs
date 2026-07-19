// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Extension methods for <see cref="IGrainFactory"/> for working with <see cref="IConnectedClients"/>.
/// </summary>
public static class ConnectedClientsGrainFactoryExtensions
{
    /// <summary>
    /// Gets the <see cref="IConnectedClients"/> grain tracking clients connected to a specific silo.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> to get the grain from.</param>
    /// <param name="siloAddress">The <see cref="SiloAddress"/> of the silo the clients are connected to.</param>
    /// <returns>The <see cref="IConnectedClients"/> grain for the silo.</returns>
    public static IConnectedClients GetConnectedClients(this IGrainFactory grainFactory, SiloAddress siloAddress) =>
        grainFactory.GetGrain<IConnectedClients>(siloAddress.ToParsableString());
}
