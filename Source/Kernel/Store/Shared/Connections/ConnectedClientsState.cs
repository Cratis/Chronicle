// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Connections;

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
