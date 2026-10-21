// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the kinds of databases Chronicle connects to.
/// </summary>
public enum StorageConnectionKind
{
    /// <summary>
    /// The cluster database, shared by every event store.
    /// </summary>
    Cluster = 0,

    /// <summary>
    /// The database of an event store.
    /// </summary>
    EventStore = 1,

    /// <summary>
    /// The database of a namespace within an event store, holding its events.
    /// </summary>
    Namespace = 2,

    /// <summary>
    /// The database holding the read models of a namespace within an event store.
    /// </summary>
    ReadModels = 3
}
