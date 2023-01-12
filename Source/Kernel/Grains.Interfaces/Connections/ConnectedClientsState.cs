// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Shared.Connections;

namespace Aksio.Cratis.Kernel.Grains.Connections;

#pragma warning disable CA1002 // Allow list

/// <summary>
/// Represents the state used for connected clients.
/// </summary>
public class ConnectedClientsState
{
    /// <summary>
    /// The name of the storage provider used for working with this type of state.
    /// </summary>
    public const string StorageProvider = "connected-clients-state";

    /// <summary>
    /// Gets or sets the connected clients.
    /// </summary>
    public List<ConnectedClient> Clients = new();
}
