// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Identifies the schema owning a database binding.
/// </summary>
internal enum DatabaseKind
{
    /// <summary>
    /// The configured cluster database.
    /// </summary>
    Cluster = 0,

    /// <summary>
    /// The event store metadata database.
    /// </summary>
    EventStore = 1,

    /// <summary>
    /// The namespace events and observer state database.
    /// </summary>
    Namespace = 2,

    /// <summary>
    /// The projected read model database.
    /// </summary>
    ReadModels = 3
}
