// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// The scoped logical identity of one read model table, with its lookup key computed once.
/// Compared by reference on the warm path, so a holder keeps one handle per role and name.
/// </summary>
/// <param name="owner">The complete logical identity.</param>
internal sealed class ReadModelTableHandle(ReadModelTableOwner owner)
{
    /// <summary>
    /// Gets the complete logical identity.
    /// </summary>
    internal ReadModelTableOwner Owner { get; } = owner;

    /// <summary>
    /// Gets the precomputed ledger lookup key.
    /// </summary>
    internal string Id { get; } = owner.Id;

    /// <summary>
    /// Gets the role of the table.
    /// </summary>
    internal ReadModelTableRole Role => Owner.Role;

    /// <summary>
    /// Gets the event store the table belongs to.
    /// </summary>
    internal string EventStore => Owner.EventStoreId;

    /// <summary>
    /// Gets the namespace the table belongs to.
    /// </summary>
    internal string Namespace => Owner.Namespace;

    /// <summary>
    /// Creates a handle for a table in a scope.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespaceName">The namespace.</param>
    /// <param name="readModelIdentifier">The read model identifier.</param>
    /// <param name="containerName">The read model's own container name.</param>
    /// <param name="role">The role of the table.</param>
    /// <param name="logicalName">The full logical table name.</param>
    /// <returns>The handle.</returns>
    internal static ReadModelTableHandle For(string eventStore, string namespaceName, string readModelIdentifier, string containerName, ReadModelTableRole role, string logicalName) =>
        new(new ReadModelTableRequest(readModelIdentifier, containerName, role, logicalName).In(eventStore, namespaceName));
}
