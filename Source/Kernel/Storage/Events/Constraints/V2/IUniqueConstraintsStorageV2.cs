// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Events.Constraints.V2;

/// <summary>
/// Defines the unwired v2 claim store for one event store, namespace and event sequence.
/// </summary>
/// <remarks>
/// Each constraint has one authority row spanning every scope and subject. Every mutation compares
/// the complete proof and changes claims atomically with that comparison; a refusal changes nothing.
/// Reads never authorize later writes. No operation creates missing authority implicitly.
/// Values must be 64-character lowercase hexadecimal opaque tokens; scopes must be explicit
/// 32-character tokens, including the caller-encoded unscoped token. No plaintext or key enters this API.
/// Invalid input throws <see cref="InvalidUniqueConstraintV2Input"/> before mutation. Cancellation
/// throws before mutation. Expected refusals are results. Live names a phase, not runtime admission.
/// </remarks>
public interface IUniqueConstraintsStorageV2
{
    /// <summary>
    /// Reads the current authority without creating it.
    /// </summary>
    /// <param name="constraint">The constraint.</param>
    /// <param name="cancellationToken">Cancellation of the read.</param>
    /// <returns>The authority, or none when missing.</returns>
    Task<Option<UniqueConstraintWriteAuthority>> GetAuthority(ConstraintName constraint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds a missing authority row once, including a revision supplied by future cutover.
    /// Existing rows are refused even when equal; no key, marker or claim is created or cleared.
    /// </summary>
    /// <param name="constraint">The constraint.</param>
    /// <param name="authority">The explicit nonempty epoch, phase and nonnegative revision.</param>
    /// <param name="cancellationToken">Cancellation of the write.</param>
    /// <returns>The initialized authority or an authority mismatch.</returns>
    Task<Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>> InitializeAuthority(ConstraintName constraint, UniqueConstraintWriteAuthority authority, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares the complete expected proof and advances its revision once, preserving its epoch
    /// and all claims. A caller reacquires the new proof. Same-phase transitions also advance.
    /// </summary>
    /// <param name="constraint">The constraint.</param>
    /// <param name="expected">The exact expected authority.</param>
    /// <param name="phase">The next Live or specific run phase.</param>
    /// <param name="cancellationToken">Cancellation of the write.</param>
    /// <returns>The new authority, or missing, mismatched or exhausted authority.</returns>
    Task<Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>> TryTransition(ConstraintName constraint, UniqueConstraintWriteAuthority expected, UniqueConstraintWritePhase phase, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads ownership without reserving a value. A same-owner claim is allowed and reports its original sequence.
    /// </summary>
    /// <param name="eventSourceId">The proposed owner.</param>
    /// <param name="definition">The constraint definition.</param>
    /// <param name="value">The opaque value token.</param>
    /// <param name="scope">The explicit opaque scope token.</param>
    /// <param name="cancellationToken">Cancellation of the read.</param>
    /// <returns>Whether the owner may claim the value and the existing sequence, or unavailable.</returns>
    Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value, UniqueConstraintStorageScope scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Snapshots values owned by the source in this scope.
    /// </summary>
    /// <param name="eventSourceId">The owner.</param>
    /// <param name="definition">The constraint definition.</param>
    /// <param name="scope">The explicit opaque scope token.</param>
    /// <param name="cancellationToken">Cancellation of the read.</param>
    /// <returns>A detached snapshot of values.</returns>
    Task<IEnumerable<UniqueConstraintValue>> GetFor(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintStorageScope scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Snapshots values for streaming; enumeration does not observe later mutations and honors both tokens.
    /// </summary>
    /// <param name="eventSourceId">The owner.</param>
    /// <param name="definition">The constraint definition.</param>
    /// <param name="scope">The explicit opaque scope token.</param>
    /// <param name="cancellationToken">Cancellation of the read and enumeration.</param>
    /// <returns>A stream over a detached snapshot.</returns>
    Task<IAsyncEnumerable<UniqueConstraintValue>> GetForStream(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintStorageScope scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Settles a claim. Another owner is refused without releasing the old claim. The same owner
    /// retains the original sequence and refreshes subject metadata. PerEventSource replaces the
    /// previous value only after a successful claim; PerValue retains every claimed value.
    /// </summary>
    /// <param name="eventSourceId">The owner.</param>
    /// <param name="definition">The constraint definition.</param>
    /// <param name="sequenceNumber">The originating sequence number.</param>
    /// <param name="value">The opaque value token.</param>
    /// <param name="scope">The explicit opaque scope token.</param>
    /// <param name="expected">The exact expected authority.</param>
    /// <param name="subject">The subject metadata to refresh, or null.</param>
    /// <param name="cancellationToken">Cancellation of the write.</param>
    /// <returns>Success or an authority or claim refusal.</returns>
    Task<Result<bool, UniqueConstraintWriteFailure>> Save(EventSourceId eventSourceId, UniqueConstraintDefinition definition, EventSequenceNumber sequenceNumber, UniqueConstraintValue value, UniqueConstraintStorageScope scope, UniqueConstraintWriteAuthority expected, Subject? subject = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes only this owner's claims in the selected scope, in either retention mode.
    /// </summary>
    /// <param name="eventSourceId">The owner.</param>
    /// <param name="definition">The constraint definition.</param>
    /// <param name="scope">The explicit opaque scope token.</param>
    /// <param name="expected">The exact expected authority.</param>
    /// <param name="cancellationToken">Cancellation of the write.</param>
    /// <returns>Success, including an absent claim, or an authority refusal.</returns>
    Task<Result<bool, UniqueConstraintWriteFailure>> Remove(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintStorageScope scope, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes one owned value in PerValue mode. PerEventSource and a different owner are no-ops.
    /// </summary>
    /// <param name="eventSourceId">The owner.</param>
    /// <param name="definition">The constraint definition.</param>
    /// <param name="value">The opaque value token.</param>
    /// <param name="scope">The explicit opaque scope token.</param>
    /// <param name="expected">The exact expected authority.</param>
    /// <param name="cancellationToken">Cancellation of the write.</param>
    /// <returns>Success, including a no-op, or an authority refusal.</returns>
    Task<Result<bool, UniqueConstraintWriteFailure>> RemoveValue(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value, UniqueConstraintStorageScope scope, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears one scope in either retention mode, never the authority row or another scope.
    /// </summary>
    /// <param name="definition">The constraint definition.</param>
    /// <param name="scope">The explicit opaque scope token.</param>
    /// <param name="expected">The exact expected authority.</param>
    /// <param name="cancellationToken">Cancellation of the write.</param>
    /// <returns>Success or an authority refusal.</returns>
    Task<Result<bool, UniqueConstraintWriteFailure>> ClearValues(UniqueConstraintDefinition definition, UniqueConstraintStorageScope scope, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all scopes in this constraint, never its authority row.
    /// </summary>
    /// <param name="definition">The constraint definition.</param>
    /// <param name="expected">The exact expected authority.</param>
    /// <param name="cancellationToken">Cancellation of the write.</param>
    /// <returns>Success or an authority refusal.</returns>
    Task<Result<bool, UniqueConstraintWriteFailure>> RemoveAllFor(UniqueConstraintDefinition definition, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the subject's claims across this constraint's scopes. It never crosses constraint authority.
    /// </summary>
    /// <param name="definition">The constraint definition whose authority covers the deletion.</param>
    /// <param name="subject">The subject to forget.</param>
    /// <param name="expected">The exact expected authority.</param>
    /// <param name="cancellationToken">Cancellation of the write.</param>
    /// <returns>Success or an authority refusal.</returns>
    Task<Result<bool, UniqueConstraintWriteFailure>> RemoveForSubject(UniqueConstraintDefinition definition, Subject subject, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken = default);
}
