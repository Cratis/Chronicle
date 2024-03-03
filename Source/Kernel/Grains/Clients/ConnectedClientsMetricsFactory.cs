// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Kernel.Grains.Clients;

/// <summary>
/// Represents an implementation of <see cref="IConnectedClientsMetricsFactory"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="ConnectedClientsMetricsFactory"/>.
/// </remarks>
/// <param name="meter">Meter for the Kernel.</param>
public class ConnectedClientsMetricsFactory(Meter meter) : IConnectedClientsMetricsFactory
{
    /// <inheritdoc/>
    public IConnectedClientsMetrics Create() => new ConnectedClientsMetrics(meter);
}
