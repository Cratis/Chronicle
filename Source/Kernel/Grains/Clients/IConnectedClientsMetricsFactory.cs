// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Clients;

/// <summary>
/// Defines a factory for creating <see cref="IConnectedClientsMetrics"/>.
/// </summary>
public interface IConnectedClientsMetricsFactory
{
    /// <summary>
    /// Create a new instance of <see cref="IConnectedClientsMetrics"/>.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> to create for.</param>
    /// <returns>A <see cref="IConnectedClientsMetrics"/>.</returns>
    IConnectedClientsMetrics Create(MicroserviceId microserviceId);
}
