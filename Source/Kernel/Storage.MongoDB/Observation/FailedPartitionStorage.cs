// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// Represents an implementation of <see cref="IFailedPartitionsStorage"/> for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ObserverStateStorage"/> class.
/// </remarks>
/// <param name="database">Provider for <see cref="IEventStoreNamespaceDatabase"/>.</param>
public class FailedPartitionStorage(IEventStoreNamespaceDatabase database) : IFailedPartitionsStorage
{
    readonly IMongoCollection<FailedPartition> _collection = database.GetCollection<FailedPartition>(WellKnownCollectionNames.FailedPartitions);
    readonly ConcurrentDictionary<string, byte> _ensuredIndexes = new();
    readonly ConcurrentDictionary<FailedPartitionId, (ObserverId Observer, Snapshot? Written)> _written = new();

    /// <summary>
    /// Gets the failed partition indexes.
    /// </summary>
    internal static IReadOnlyList<CreateIndexModel<FailedPartition>> Indexes { get; } =
    [
        new(Builders<FailedPartition>.IndexKeys.Ascending(_ => _.ObserverId), new CreateIndexOptions { Name = "observerId", Background = true })
    ];

    /// <inheritdoc/>
    public ISubject<IEnumerable<FailedPartition>> ObserveAllFor(ObserverId? observerId = default)
    {
        if (observerId == default) return _collection.Observe();
        return _collection.Observe(_ => _.ObserverId == observerId);
    }

    /// <inheritdoc/>
    public async Task Save(ObserverId observerId, FailedPartitions failedPartitions)
    {
        await EnsureIndexes().ConfigureAwait(false);

        // Everything the observer holds is handed over on every save, and a recovery saves several times per partition,
        // so with hundreds of failed partitions writing it all one round trip at a time is quadratic. Only what differs
        // from what was last written is sent, and all of it in one request.
        var writes = new List<WriteModel<FailedPartition>>();
        var pending = new List<(FailedPartitionId Id, Snapshot? Written)>();
        var current = new HashSet<FailedPartitionId>();

        foreach (var failedPartition in failedPartitions.ResolvedPartitions)
        {
            current.Add(failedPartition.Id);
            if (_written.TryGetValue(failedPartition.Id, out var written) && written.Written is null)
            {
                continue;
            }

            writes.Add(new DeleteOneModel<FailedPartition>(Builders<FailedPartition>.Filter.Eq(_ => _.Id, failedPartition.Id)));
            pending.Add((failedPartition.Id, null));
        }

        foreach (var failedPartition in failedPartitions.Partitions)
        {
            current.Add(failedPartition.Id);
            var snapshot = Snapshot.Of(failedPartition);
            if (_written.TryGetValue(failedPartition.Id, out var written) && snapshot.Equals(written.Written))
            {
                continue;
            }

            writes.Add(new ReplaceOneModel<FailedPartition>(Builders<FailedPartition>.Filter.Eq(_ => _.Id, failedPartition.Id), failedPartition) { IsUpsert = true });
            pending.Add((failedPartition.Id, snapshot));
        }

        if (writes.Count > 0)
        {
            await _collection.BulkWriteAsync(writes, new BulkWriteOptions { IsOrdered = false }).ConfigureAwait(false);
        }

        foreach (var (id, snapshot) in pending)
        {
            _written[id] = (observerId, snapshot);
        }

        // What the observer no longer holds in either list is not going to be written again.
        foreach (var (id, written) in _written)
        {
            if (written.Observer == observerId && !current.Contains(id))
            {
                _written.TryRemove(id, out _);
            }
        }
    }

    /// <inheritdoc/>
    public async Task RemoveAllFor(ObserverId observerId)
    {
        await EnsureIndexes().ConfigureAwait(false);
        await _collection.DeleteManyAsync(_ => _.ObserverId == observerId).ConfigureAwait(false);
        foreach (var (id, written) in _written)
        {
            if (written.Observer == observerId)
            {
                _written.TryRemove(id, out _);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<FailedPartitions> GetFor(ObserverId? observerId)
    {
        await EnsureIndexes().ConfigureAwait(false);
        using var cursor = observerId is null ?
            await _collection.FindAsync(_ => true).ConfigureAwait(false) :
            await _collection.FindAsync(_ => _.ObserverId == observerId).ConfigureAwait(false);

        return new FailedPartitions
        {
            Partitions = await cursor.ToListAsync()
        };
    }

    /// <inheritdoc/>
    public async Task<FailedPartitions> GetFor(IEnumerable<ObserverId> observerIds)
    {
        await EnsureIndexes().ConfigureAwait(false);
        var ids = observerIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new FailedPartitions();
        }

        using var cursor = await _collection.FindAsync(_ => ids.Contains(_.ObserverId)).ConfigureAwait(false);

        return new FailedPartitions
        {
            Partitions = await cursor.ToListAsync()
        };
    }

    Task EnsureIndexes() => _collection.EnsureIndexesOnceAsync(_ensuredIndexes, Indexes.ToArray());

    /// <summary>
    /// What was written of a failed partition, kept to tell whether it has changed since. Equality is by value for what
    /// can change in place and by reference for the attempts, which are only ever added.
    /// </summary>
    /// <param name="Partition">The partition.</param>
    /// <param name="ObserverId">The observer it belongs to.</param>
    /// <param name="AttemptCount">The number of attempts.</param>
    /// <param name="FirstAttempt">The first attempt.</param>
    /// <param name="LastAttempt">The last attempt.</param>
    /// <param name="IsResolved">Whether it is resolved.</param>
    /// <param name="IsQuarantined">Whether it is quarantined.</param>
    /// <param name="AttemptsBeforeBudgetReset">The attempts before the retry budget was reset.</param>
    /// <param name="RecoveryCursor">The recovery cursor.</param>
    /// <param name="RecoveryOwner">The recovery owner.</param>
    /// <param name="Hold">The hold.</param>
    /// <param name="Deferral">The deferral.</param>
    /// <param name="ReplayEpoch">The replay epoch.</param>
    /// <param name="WebhookDisposition">The webhook failure disposition.</param>
    /// <param name="WebhookRecoveryId">The webhook recovery identifier.</param>
    sealed record Snapshot(
        Key Partition,
        ObserverId ObserverId,
        int AttemptCount,
        FailedPartitionAttempt? FirstAttempt,
        FailedPartitionAttempt? LastAttempt,
        bool IsResolved,
        bool IsQuarantined,
        int AttemptsBeforeBudgetReset,
        FailedPartitionRecoveryCursor? RecoveryCursor,
        FailedPartitionRecoveryOwner? RecoveryOwner,
        FailedPartitionHold? Hold,
        PartitionDeferral? Deferral,
        ReplayEpoch? ReplayEpoch,
        WebhookFailureDisposition? WebhookDisposition,
        Guid? WebhookRecoveryId)
    {
        internal static Snapshot Of(FailedPartition failedPartition) => new(
            failedPartition.Partition,
            failedPartition.ObserverId,
            failedPartition.Attempts.Count(),
            failedPartition.Attempts.FirstOrDefault(),
            failedPartition.Attempts.LastOrDefault(),
            failedPartition.IsResolved,
            failedPartition.IsQuarantined,
            failedPartition.AttemptsBeforeBudgetReset,
            failedPartition.RecoveryCursor,
            failedPartition.RecoveryOwner,
            failedPartition.Hold,
            failedPartition.Deferral,
            failedPartition.ReplayEpoch,
            failedPartition.WebhookDisposition,
            failedPartition.WebhookRecoveryId);
    }
}
