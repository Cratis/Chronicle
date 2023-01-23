// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Kernel.MongoDB.Clients;

/// <summary>
/// Represents the connected client state for MongoDB.
/// </summary>
/// <param name="Id">The <see cref="MicroserviceId"/>.</param>
/// <param name="Clients">Collection of <see cref="ConnectedClient"/>.</param>
public record MongoDBConnectedClientState(MicroserviceId Id, IEnumerable<ConnectedClient> Clients);
