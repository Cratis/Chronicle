// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Aksio.Cratis.Kernel.Grains.Clients;

/// <summary>
/// Represents an implementation of <see cref="IConnectedClientsMetrics"/>.
/// </summary>
public class ConnectedClientsMetrics : IConnectedClientsMetrics
{
#pragma warning disable IDE0052
    readonly ObservableGauge<int> _connectedClients;

    int _connectedClientsCount;

    /// <summary>
    /// Initializes a new instance of <see cref="ConnectedClientsMetrics"/>.
    /// </summary>
    /// <param name="meter">Meter for the Kernel.</param>
    public ConnectedClientsMetrics(Meter meter)
    {
        _connectedClients = meter.CreateObservableGauge($"cratis-connected-clients", () => _connectedClientsCount, description: "Number of connected clients");
    }

    /// <inheritdoc/>
    public void SetConnectedClients(int count) => _connectedClientsCount = count;
}
