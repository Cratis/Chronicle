// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Converts stored facts without adding query endpoints to the read model.
/// </summary>
internal static class OccurrenceSnapshotConverters
{
    /// <summary>
    /// Converts current delivery facts.
    /// </summary>
    /// <param name="occurrence">The occurrence.</param>
    /// <param name="receipts">The receipts.</param>
    /// <returns>The read surface.</returns>
    internal static OccurrenceSnapshot ToSnapshot(Occurrence occurrence, IEnumerable<EffectReceiptDetails> receipts) => new(
        occurrence.Id.Value, occurrence.Trigger.Value, occurrence.Kind, occurrence.Payload, occurrence.SchemaRevision, occurrence.RecordedAt, occurrence.Due, occurrence.State, occurrence.Attempts, occurrence.Epoch, occurrence.Owner, occurrence.LeaseExpiry, occurrence.LastError, occurrence.NextAttempt, occurrence.RetriedBy, occurrence.RetriedAt, receipts);

    /// <summary>
    /// Reads receipts in every effect namespace for application-scoped work.
    /// </summary>
    /// <param name="occurrence">The occurrence.</param>
    /// <param name="store">The store.</param>
    /// <param name="namespace">The optional namespace.</param>
    /// <returns>The status snapshot.</returns>
    /// <exception cref="OccurrencesStorageNotSupported">The provider has no receipt storage.</exception>
    internal static async Task<OccurrenceSnapshot> WithReceipts(Occurrence occurrence, IEventStoreStorage store, string? @namespace)
    {
        var namespaces = @namespace is null ? (await store.Namespaces.GetAll()).Select(state => state.Name) : [new Concepts.EventStoreNamespaceName(@namespace)];
        var receipts = new List<EffectReceiptDetails>();
        foreach (var name in namespaces)
        {
            var sequence = store.GetNamespace(name).GetEventSequence(EventSequenceId.Log);
            var receiptStorage = sequence as IEffectReceiptStorage ?? throw new OccurrencesStorageNotSupported();
            receipts.AddRange((await receiptStorage.GetEffectReceipts(occurrence.Id)).Select(EffectReceiptConverters.ToDetails));
        }

        return ToSnapshot(occurrence, receipts);
    }

    /// <summary>
    /// Creates a subscription-owned, cross-silo durable status stream.
    /// </summary>
    /// <param name="storage">The kernel storage.</param>
    /// <param name="key">The queue scope.</param>
    /// <returns>The observable query subject.</returns>
    internal static ISubject<IEnumerable<OccurrenceSnapshot>> Observe(IStorage storage, OccurrenceQueueKey key)
    {
        // Each subscription owns its timer and serializes reads. Disposal stops all further work.
        var source = Observable.Create<IEnumerable<OccurrenceSnapshot>>(async (observer, cancellationToken) =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var snapshots = new List<OccurrenceSnapshot>();
                foreach (var occurrence in await key.Storage(storage).GetForTrigger(new(key.Trigger)).WaitAsync(cancellationToken))
                {
                    snapshots.Add(await WithReceipts(occurrence, storage.GetEventStore(new(key.EventStore)), key.Namespace).WaitAsync(cancellationToken));
                }

                observer.OnNext(snapshots);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        });
        return Subject.Create<IEnumerable<OccurrenceSnapshot>>(Observer.Create<IEnumerable<OccurrenceSnapshot>>(_ => { }), source);
    }
}
