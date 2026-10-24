// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clustering;

namespace Cratis.Chronicle.Storage.SingleWriter;

/// <summary>
/// Atomically leases one kernel storage using the database clock, independently of cluster identity.
/// </summary>
public interface ISingleWriterLeaseStorage
{
    /// <summary>
    /// Acquires an absent or expired lease, or extends the same owner's lease.
    /// </summary>
    /// <param name="claim">The requested lease.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The acquisition outcome and current holder.</returns>
    Task<SingleWriterLeaseAttempt> TryAcquire(SingleWriterLeaseClaim claim, CancellationToken cancellationToken);

    /// <summary>
    /// Renews only an unexpired lease belonging to the owner.
    /// </summary>
    /// <param name="owner">The owner identity.</param>
    /// <param name="ttl">The new lifetime.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Whether the lease was renewed; storage failures throw.</returns>
    Task<bool> Renew(SingleWriterLeaseOwner owner, TimeSpan ttl, CancellationToken cancellationToken);

    /// <summary>
    /// Expires the lease if it still belongs to the owner.
    /// </summary>
    /// <param name="owner">The owner identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completion task.</returns>
    Task Release(SingleWriterLeaseOwner owner, CancellationToken cancellationToken);
}
