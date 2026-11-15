// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Defines the replication topology of the MongoDB server Chronicle stores its data in.
/// </summary>
/// <remarks>
/// Linearizable reads and majority writes need replication. A standalone MongoDB server refuses linearizable reads, so
/// anything that relies on them has to know the topology before it promises a guarantee the server cannot give.
/// </remarks>
public interface IMongoDBTopology
{
    /// <summary>
    /// Gets a value indicating whether the server is known to be a standalone MongoDB server without replication.
    /// </summary>
    /// <remarks>
    /// <see langword="false"/> while the topology is unknown: production requires a replica set, so an unreachable
    /// server is not taken as evidence of a weaker topology.
    /// </remarks>
    bool IsStandalone { get; }
}
