// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.Grains.Clients;

/// <summary>
/// Represents an implementation of <see cref="IConnectedClientsMetrics"/>.
/// </summary>
public class ConnectedClientsMetrics : IConnectedClientsMetrics
{
#pragma warning disable IDE0052
    readonly ObservableGauge<int> _connectedClients;
#pragma warning restore IDE0052

    int _connectedClientsCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectedClientsMetrics"/>.
    /// </summary>
    /// <param name="meter">Meter for the Kernel.</param>
    public ConnectedClientsMetrics(Meter meter)
    {
        _connectedClients = meter.CreateObservableGauge("chronicle-connected-clients", () => _connectedClientsCount, description: "Number of connected clients");
    }

    /// <inheritdoc/>
    public void SetConnectedClients(int count) => _connectedClientsCount = count;
}
