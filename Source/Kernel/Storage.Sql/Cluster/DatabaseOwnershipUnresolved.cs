// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// The exception that is thrown when a shared database may contain an identity's data.
/// </summary>
/// <param name="identity">The unresolved logical identity.</param>
/// <param name="database">The shared physical database.</param>
/// <param name="claimants">The competing identities.</param>
#pragma warning disable CA1064 // Kernel-only diagnostic; the operator repairs a binding rather than catching it.
internal sealed class DatabaseOwnershipUnresolved(DatabaseIdentity identity, string database, IEnumerable<DatabaseIdentity> claimants)
    : Exception($"DatabaseOwnershipUnresolved: {identity} cannot own shared database '{database}'. Claimants: {string.Join(", ", claimants)}. Stop the kernels, copy this identity's data to a separate database and insert its DatabaseNames binding before restarting.");
