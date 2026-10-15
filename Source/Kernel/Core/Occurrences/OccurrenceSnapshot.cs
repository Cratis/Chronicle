// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Exposes durable delivery state and the winning effect receipts.
/// </summary>
/// <param name="Id">The stable occurrence identity.</param>
/// <param name="Trigger">The trigger identity.</param>
/// <param name="Kind">The source kind.</param>
/// <param name="Payload">The validated plaintext JSON.</param>
/// <param name="SchemaRevision">The admitted schema revision.</param>
/// <param name="RecordedAt">The recording instant.</param>
/// <param name="Due">The schedule instant, if any.</param>
/// <param name="State">The delivery state.</param>
/// <param name="Attempts">The attempt count.</param>
/// <param name="Epoch">The fencing epoch.</param>
/// <param name="Owner">The connection holding the lease.</param>
/// <param name="LeaseExpiry">The kernel expiry.</param>
/// <param name="LastError">The most recent failure.</param>
/// <param name="NextAttempt">The next retry instant.</param>
/// <param name="RetriedBy">The most recent operator.</param>
/// <param name="RetriedAt">The operator re-arm instant.</param>
/// <param name="Receipts">The durable effects, not atomic with this delivery snapshot.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Occurrences)]
public record OccurrenceSnapshot(
    string Id,
    string Trigger,
    OccurrenceKind Kind,
    string Payload,
    long SchemaRevision,
    DateTimeOffset RecordedAt,
    DateTimeOffset? Due,
    OccurrenceState State,
    int Attempts,
    long Epoch,
    string? Owner,
    DateTimeOffset? LeaseExpiry,
    string? LastError,
    DateTimeOffset? NextAttempt,
    string? RetriedBy,
    DateTimeOffset? RetriedAt,
    IEnumerable<EffectReceiptDetails> Receipts)
{
    /// <summary>
    /// Reads one occurrence and its receipts. Null namespace selects application scope.
    /// </summary>
    /// <param name="eventStore">The target store.</param>
    /// <param name="namespace">The namespace or null for application scope.</param>
    /// <param name="occurrenceId">The occurrence identity.</param>
    /// <param name="storage">The kernel storage.</param>
    /// <returns>The snapshot, or null.</returns>
    /// <param name="applicationScope">Selects application storage over the namespace.</param>
    public static async Task<OccurrenceSnapshot?> GetOccurrence(EventStoreName eventStore, string? @namespace, string occurrenceId, IStorage storage, bool applicationScope = false)
    {
        var scope = applicationScope ? null : @namespace;
        var key = new OccurrenceQueueKey(eventStore.Value, scope, string.Empty);
        var occurrence = await key.Storage(storage).Get(new(occurrenceId));
        return occurrence is null ? null : await OccurrenceSnapshotConverters.WithReceipts(occurrence, storage.GetEventStore(eventStore), scope);
    }

    /// <summary>
    /// Observes durable queue snapshots across silos, including time-based claimability changes.
    /// </summary>
    /// <param name="eventStore">The target store.</param>
    /// <param name="namespace">The namespace or null for application scope.</param>
    /// <param name="trigger">The trigger identity.</param>
    /// <param name="storage">The kernel storage.</param>
    /// <returns>A subscription-owned wake stream.</returns>
    /// <param name="applicationScope">Selects application storage over the namespace.</param>
    public static ISubject<IEnumerable<OccurrenceSnapshot>> ObserveOccurrences(EventStoreName eventStore, string? @namespace, string trigger, IStorage storage, bool applicationScope = false) =>
        OccurrenceSnapshotConverters.Observe(storage, new(eventStore.Value, applicationScope ? null : @namespace, trigger));
}
