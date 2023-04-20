// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Clients;

/// <summary>
/// Defines a system that can track metrics for connected clients.
/// </summary>
public interface IConnectedClientsMetrics
{
    /// <summary>
    /// Track a client that has connected.
    /// </summary>
    /// <param name="count">Number of connected clients.</param>
    void SetConnectedClients(int count);
}
