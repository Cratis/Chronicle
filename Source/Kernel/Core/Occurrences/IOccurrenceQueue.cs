// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Defines one non-reentrant, storage-fenced occurrence queue. Snapshots are JSON infrastructure facts.
/// </summary>
public interface IOccurrenceQueue : IGrainWithStringKey
{
    /// <summary>
    /// Records validated ingress.
    /// </summary>
    /// <param name="idempotencyId">The retry identity.</param>
    /// <param name="payload">The payload JSON.</param>
    /// <param name="kind">The ingress kind.</param>
    /// <returns>The durable outcome.</returns>
    Task<OccurrenceIngressResult> Record(string idempotencyId, string payload, OccurrenceKind kind);

    /// <summary>
    /// Claims due work.
    /// </summary>
    /// <param name="worker">The connection identity.</param>
    /// <param name="leaseSeconds">The duration, zero for default.</param>
    /// <param name="max">The requested batch size.</param>
    /// <returns>The claimed snapshots.</returns>
    Task<string> Claim(string worker, int leaseSeconds, int max);

    /// <summary>
    /// Renews a fenced lease.
    /// </summary>
    /// <param name="id">The occurrence.</param>
    /// <param name="epoch">The fencing epoch.</param>
    /// <param name="leaseSeconds">The duration, zero for default.</param>
    /// <returns>The transition outcome.</returns>
    Task<OccurrenceLeaseResult> Renew(string id, long epoch, int leaseSeconds);

    /// <summary>
    /// Completes a fenced lease.
    /// </summary>
    /// <param name="id">The occurrence.</param>
    /// <param name="epoch">The fencing epoch.</param>
    /// <returns>The transition outcome.</returns>
    Task<OccurrenceLeaseResult> Complete(string id, long epoch);

    /// <summary>
    /// Fails a fenced lease.
    /// </summary>
    /// <param name="id">The occurrence.</param>
    /// <param name="epoch">The fencing epoch.</param>
    /// <param name="error">The failure description.</param>
    /// <returns>The transition outcome.</returns>
    Task<OccurrenceLeaseResult> Fail(string id, long epoch, string error);

    /// <summary>
    /// Atomically re-arms and audits quarantined work.
    /// </summary>
    /// <param name="id">The occurrence.</param>
    /// <param name="principal">The authenticated operator.</param>
    /// <returns>Whether work was re-armed.</returns>
    Task<bool> Retry(string id, string principal);

    /// <summary>
    /// Reads one durable snapshot without blocking mutations.
    /// </summary>
    /// <param name="id">The occurrence.</param>
    /// <returns>The snapshot, or null.</returns>
    [AlwaysInterleave]
    Task<string?> Get(string id);

    /// <summary>
    /// Reads the queue's durable snapshots, also acting as a worker wake signal.
    /// </summary>
    /// <returns>The snapshots.</returns>
    [AlwaysInterleave]
    Task<string> GetAll();

    /// <summary>
    /// Restores reminder and timer wake-ups from durable work.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Wake();
}
