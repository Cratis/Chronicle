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

    /// <summary>
    /// Compare-and-set of the lifecycle state; only a row in the expected state changes.
    /// </summary>
    /// <param name="id">The complete tuple's digest.</param>
    /// <param name="expected">The state the row must currently be in.</param>
    /// <param name="next">The state to move it to.</param>
    /// <returns>Whether the row was in the expected state and was updated.</returns>
    Task<bool> Transition(string id, ReadModelTableBindingState expected, ReadModelTableBindingState next);

    /// <summary>
    /// Compare-and-set of the replay attempt marker on a row.
    /// </summary>
    /// <param name="id">The complete tuple's digest.</param>
    /// <param name="expected">The attempt the row must currently carry, or null when none is set.</param>
    /// <param name="attempt">The attempt to store, or null to clear it.</param>
    /// <returns>Whether the row carried the expected attempt and was updated.</returns>
    Task<bool> SetReplayAttempt(string id, string? expected, string? attempt);
}
