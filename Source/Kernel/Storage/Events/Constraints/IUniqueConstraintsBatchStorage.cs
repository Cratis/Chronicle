// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Events.Constraints;

/// <summary>
/// Defines a <see cref="IUniqueConstraintsStorage"/> that can check and update the claims of many events in one
/// round trip.
/// </summary>
/// <remarks>
/// A batch append checks every event before appending any of them and updates the index for every event after,
/// which through the single-item operations costs a storage round trip per event and constraint - what made large
/// batches slow. The operations here answer and do exactly what the single-item ones would, in fewer round trips.
/// A storage that does not implement this is used one item at a time.
/// </remarks>
public interface IUniqueConstraintsBatchStorage : IUniqueConstraintsStorage
{
    /// <summary>
    /// Check several claims at once, answering for each exactly what
    /// <see cref="IUniqueConstraintsStorage.IsAllowed"/> would.
    /// </summary>
    /// <param name="claims">The <see cref="UniqueConstraintClaim">claims</see> to check.</param>
    /// <param name="definition"><see cref="UniqueConstraintDefinition"/> to check for.</param>
    /// <param name="scopeKey">The resolved scope the claims are made in.</param>
    /// <param name="cancellationToken">Cancellation of the read.</param>
    /// <returns>One answer per claim, in the order of <paramref name="claims"/>.</returns>
    Task<IReadOnlyList<(bool IsAllowed, EventSequenceNumber SequenceNumber)>> IsAllowedMany(IReadOnlyList<UniqueConstraintClaim> claims, UniqueConstraintDefinition definition, string scopeKey = "", CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies the index changes of appended events, in order, as each of <see cref="IUniqueConstraintsStorage.Save"/>,
    /// <see cref="IUniqueConstraintsStorage.Remove"/> and <see cref="IUniqueConstraintsStorage.RemoveValue"/> would.
    /// </summary>
    /// <param name="definition">The <see cref="UniqueConstraintDefinition"/> the changes belong to.</param>
    /// <param name="changes">The <see cref="UniqueConstraintIndexChange">changes</see>, in the order the events were appended.</param>
    /// <param name="cancellationToken">Cancellation of the writes.</param>
    /// <returns>
    /// The positions, in <paramref name="changes"/>, of the claims refused because another event source holds the
    /// value - what <see cref="IUniqueConstraintsStorage.Save"/> reports by throwing
    /// <see cref="DuplicateUniqueConstraintValue"/>. Every other change is applied.
    /// </returns>
    Task<IReadOnlyCollection<int>> ApplyChanges(UniqueConstraintDefinition definition, IReadOnlyList<UniqueConstraintIndexChange> changes, CancellationToken cancellationToken = default);
}
