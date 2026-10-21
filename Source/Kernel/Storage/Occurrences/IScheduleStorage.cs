// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// Defines direct infrastructure storage for durable application work.
/// </summary>
public interface IScheduleStorage
{
    /// <summary>
    /// Reads the durable definition.
    /// </summary>
    /// <param name="token">The stable token.</param>
    /// <returns>The durable storage outcome.</returns>
    Task<ScheduleDefinition?> GetDefinition(ScheduleToken token);

    /// <summary>
    /// Inserts the first definition and EffectiveFrom together, without overwriting a registration.
    /// </summary>
    /// <param name="definition">The definition including EffectiveFrom.</param>
    /// <returns>The durable storage outcome.</returns>
    Task<bool> TryAddDefinition(ScheduleDefinition definition);

    /// <summary>
    /// Compare-and-sets a definition revision. The engine supplies explicit migration semantics.
    /// </summary>
    /// <param name="expectedRevision">The current revision.</param>
    /// <param name="definition">The replacement definition.</param>
    /// <returns>The durable storage outcome.</returns>
    Task<bool> TryUpdateDefinition(long expectedRevision, ScheduleDefinition definition);

    /// <summary>
    /// Reads the durable cursor.
    /// </summary>
    /// <param name="token">The stable token.</param>
    /// <returns>The durable storage outcome.</returns>
    Task<ScheduleCursor?> GetCursor(ScheduleToken token);

    /// <summary>
    /// Creates a cursor if absent; both revisions initially match.
    /// </summary>
    /// <param name="cursor">The initial cursor derived from durable facts.</param>
    /// <returns>The durable storage outcome.</returns>
    Task<bool> TryAddCursor(ScheduleCursor cursor);

    /// <summary>
    /// Stamps an existing cursor and increments Version without changing AppliedRevision.
    /// </summary>
    /// <param name="token">The stable token.</param>
    /// <param name="revision">The new registered revision.</param>
    /// <returns>The durable storage outcome.</returns>
    Task RegisterRevision(ScheduleToken token, long revision);

    /// <summary>
    /// Atomically inserts due identities, records skips, applies a directive and compare-and-sets the cursor. A lost version writes nothing.
    /// </summary>
    /// <param name="turn">The computed turn.</param>
    /// <returns>The durable storage outcome.</returns>
    Task<bool> TryCommitTurn(ScheduleTurn turn);

    /// <summary>
    /// Reads recorded skip explanations.
    /// </summary>
    /// <param name="token">The stable token.</param>
    /// <returns>The durable storage outcome.</returns>
    Task<IEnumerable<SkippedRange>> GetSkippedRanges(ScheduleToken token);
}
