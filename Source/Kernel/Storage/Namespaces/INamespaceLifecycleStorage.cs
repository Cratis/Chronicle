// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Namespaces;

/// <summary>
/// Stores namespace lifecycle tombstones outside the namespace database.
/// Rows are never deleted, and revisions are monotonic across name reuse.
/// </summary>
public interface INamespaceLifecycleStorage
{
    /// <summary>Gets the control state, without creating or opening a namespace.</summary>
    /// <param name="name">The canonical namespace name.</param>
    /// <returns>The control state, or null for an uninitialized legacy namespace.</returns>
    Task<NamespaceLifecycle?> Get(EventStoreNamespaceName name);

    /// <summary>Atomically installs the next revision, conditional on the exact previous revision.</summary>
    /// <param name="name">The canonical namespace name.</param>
    /// <param name="expectedRevision">The exact previous revision, or null to require an absent row.</param>
    /// <param name="next">The next control state.</param>
    /// <returns>Whether this compare-and-swap won. A stale revision never changes control state.</returns>
    Task<bool> TryWrite(EventStoreNamespaceName name, long? expectedRevision, NamespaceLifecycle next);
}
