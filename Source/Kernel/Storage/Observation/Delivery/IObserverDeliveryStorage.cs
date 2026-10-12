// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Sequencing;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Defines atomic observer delivery root CAS and row storage, without coupling to an observer implementation.
/// </summary>
public interface IObserverDeliveryStorage
{
    /// <summary>
    /// Reads the current root, or null if none exists. Invalid stored values fail closed with CorruptDeliveryRow.
    /// </summary>
    /// <param name="observer">The observer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current root or null.</returns>
    Task<ObserverDeliveryRoot?> GetRoot(ObserverId observer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates, writes the root first under CAS, applies rows, then commits as one atomic unit.
    /// </summary>
    /// <param name="commit">The immutable commit.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The committed revision, or a conflict when the root CAS matches nothing.</returns>
    /// <remarks>
    /// Expected None creates a root strictly; otherwise update only the expected revision. A strict row insert
    /// collision rolls back the root and every row and throws DeliveryRowConflict. Other faults throw, never
    /// degrade to success. LastCommit must be new for each attempt; after an ambiguous outcome, read the root
    /// and compare this token. Deletes are idempotent. Existing S range disjointness is the engine's job under CAS.
    /// </remarks>
    /// <exception cref="InvalidDeliveryCommit">A commit invariant fails before I/O.</exception>
    /// <exception cref="DeliveryValueOutOfRange">A value is out of range before I/O.</exception>
    /// <exception cref="DeliveryRowConflict">A strict row insert collides and the entire commit rolls back.</exception>
    Task<Result<DeliveryRevision, DeliveryRevisionConflict>> Commit(DeliveryCommit commit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads an F row identified by generation and hash within the observer.
    /// </summary>
    /// <param name="observer">The observer.</param>
    /// <param name="generation">The row generation.</param>
    /// <param name="key">The key hash.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parked key, or null if absent.</returns>
    Task<ParkedKey?> GetParkedKey(ObserverId observer, DeliveryGeneration generation, SequencingKeyHash key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pages non-quarantined F rows in the slices, ordered by RetryDue, ReadyTicket, then ordinal KeyHash.
    /// </summary>
    /// <param name="observer">The observer.</param>
    /// <param name="generation">The row generation.</param>
    /// <param name="slices">The half-open slice range.</param>
    /// <param name="after">The strictly-after cursor, or null for the first page.</param>
    /// <param name="limit">The size clamped by DeliveryPaging to one through one thousand.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ordered page; build the next cursor from its last item.</returns>
    Task<ParkedKeyPage> GetEligibleParkedKeys(ObserverId observer, DeliveryGeneration generation, SliceRange slices, EligibleParkedKeyCursor? after, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pages quarantined F rows ordered by ordinal KeyHash.
    /// </summary>
    /// <param name="observer">The observer.</param>
    /// <param name="generation">The row generation.</param>
    /// <param name="after">The strictly-after hash, or null for the first page.</param>
    /// <param name="limit">The size clamped by DeliveryPaging to one through one thousand.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ordered page; its last hash is the next cursor.</returns>
    Task<ParkedKeyPage> GetQuarantinedParkedKeys(ObserverId observer, DeliveryGeneration generation, SequencingKeyHash? after, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pages S rows ordered by ordinal KeyHash then From, or by From for one filtered key.
    /// </summary>
    /// <param name="observer">The observer.</param>
    /// <param name="generation">The row generation.</param>
    /// <param name="key">The optional key hash filter.</param>
    /// <param name="after">The strictly-after cursor, or null for the first page.</param>
    /// <param name="limit">The size clamped by DeliveryPaging to one through one thousand.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ordered page; build the next cursor from its last item.</returns>
    Task<SkipRangePage> GetSkipRanges(ObserverId observer, DeliveryGeneration generation, SequencingKeyHash? key, SkipRangeCursor? after, int limit, CancellationToken cancellationToken = default);
}
