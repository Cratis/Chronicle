// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// A metadata-only ledger in one resolved database/schema, independent of dynamic table access.
/// </summary>
internal interface IReadModelTableNameBindings
{
    /// <summary>
    /// Serializes inspection and reservation against independent resolvers.
    /// </summary>
    /// <param name="action">The ownership operation.</param>
    /// <returns>The result after the operation commits.</returns>
    Task<ReadModelTableNameBinding> UnderLock(Func<Task<ReadModelTableNameBinding>> action);

    /// <summary>Loads a digest-keyed row; its full identity still requires validation.</summary>
    /// <param name="id">The complete tuple's digest.</param>
    /// <returns>The stored row, if any.</returns>
    Task<ReadModelTableNameBinding?> Find(string id);

    /// <summary>Reads all permanent reservations, including removal tombstones.</summary>
    /// <returns>The stored ownership assignments.</returns>
    Task<IReadOnlyList<ReadModelTableNameBinding>> Read();

    /// <summary>
    /// Commits an immutable ownership assignment, or returns false on a unique-key race.
    /// </summary>
    /// <param name="binding">The assignment to commit.</param>
    /// <returns>Whether this operation inserted the row.</returns>
    Task<bool> TryInsert(ReadModelTableNameBinding binding);
}
