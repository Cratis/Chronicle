// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;

namespace Aksio.Cratis.Kernel.MongoDB.Clients;

/// <summary>
/// Represents the connected client state for MongoDB.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Clients">Collection of <see cref="ConnectedClient"/>.</param>
public record ConnectedClientsState(uint Id, IEnumerable<ConnectedClient> Clients);
