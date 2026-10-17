// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Defines bounded persistence for one definition-change row family. Nothing calls this store before initialization.
/// </summary>
/// <typeparam name="T">The payload type.</typeparam>
public interface IObserverDefinitionChangeRows<T>
{
    /// <summary>
    /// Reads one row without treating a missing row as a completed job.
    /// </summary>
    /// <param name="key">The row identity.</param>
    /// <returns>The row, or null.</returns>
    Task<ObserverDefinitionChangeRow<T>?> Get(ObserverDefinitionChangeRowKey key);

    /// <summary>
    /// Inserts a version-zero row only if it does not exist. Repeated harvests never replace an existing row.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <returns>Whether the row was inserted.</returns>
    Task<bool> TryInsert(ObserverDefinitionChangeRow<T> row);

    /// <summary>
    /// Atomically replaces a nonterminal row at the expected version. The new version must be expected + 1;
    /// state and a recorded position cannot move backwards. Terminal rows cannot even be rewritten at equal state.
    /// </summary>
    /// <param name="row">The replacement with the same identity.</param>
    /// <param name="expectedVersion">The version read by the writer.</param>
    /// <returns>Whether the replacement was applied.</returns>
    Task<bool> TryReplace(ObserverDefinitionChangeRow<T> row, long expectedVersion);

    /// <summary>
    /// Reads at most 1,000 rows, ordered by an opaque stable cursor, optionally across all revisions for harvesting.
    /// Reads do not delete, settle, or initialize anything.
    /// </summary>
    /// <param name="observer">The complete observer identity.</param>
    /// <param name="revision">A revision, or null to harvest all revisions.</param>
    /// <param name="after">The preceding page's cursor, or null for the first page.</param>
    /// <param name="limit">The requested page size, clamped to 0 through 1,000.</param>
    /// <returns>The bounded page and continuation cursor.</returns>
    Task<ObserverDefinitionChangePage<T>> ReadPage(ObserverKey observer, ObserverTypeSetRevision? revision, string? after, int limit);

    /// <summary>
    /// Deletes precisely the version consumed by the caller. The caller must apply the outcome and settle its
    /// origin first, delete progress second, and delete terminal inventory last (decision 0011 section 3).
    /// </summary>
    /// <param name="key">The row identity.</param>
    /// <param name="expectedVersion">The consumed version.</param>
    /// <returns>Whether that version was deleted.</returns>
    Task<bool> TryDelete(ObserverDefinitionChangeRowKey key, long expectedVersion);
}
