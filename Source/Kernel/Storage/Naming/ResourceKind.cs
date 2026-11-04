// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Represents the kind of resource an owner uses.
/// </summary>
public enum ResourceKind
{
    /// <summary>
    /// The database holding the cross-namespace state of an event store.
    /// </summary>
    EventStoreDatabase = 0,

    /// <summary>
    /// The database holding the event sequences of a namespace.
    /// </summary>
    NamespaceDatabase = 1,

    /// <summary>
    /// The database holding the read models of a namespace.
    /// </summary>
    ReadModelDatabase = 2,

    /// <summary>
    /// The storage holding the jobs of an owner.
    /// </summary>
    Jobs = 3,

    /// <summary>
    /// The key of a grain addressed by the owner.
    /// </summary>
    GrainKey = 4,

    /// <summary>
    /// A name the kernel keeps for itself.
    /// </summary>
    Kernel = 5
}
