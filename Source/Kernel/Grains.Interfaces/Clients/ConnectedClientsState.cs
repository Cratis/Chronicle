// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;

namespace Aksio.Cratis.Kernel.Grains.Clients;

#pragma warning disable CA1002 // Allow list

/// <summary>
/// Represents the state used for connected clients.
/// </summary>
public class ConnectedClientsState
{
    /// <summary>
    /// Gets or sets the connected clients.
    /// </summary>
    public IList<ConnectedClient> Clients { get; set; } = new List<ConnectedClient>();
}
