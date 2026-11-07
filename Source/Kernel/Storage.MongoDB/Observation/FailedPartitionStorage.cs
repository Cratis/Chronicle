// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using MongoDB.Bson;
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
    readonly ConcurrentDictionary<FailedPartitionId, (ObserverId Observer, byte[]? Document)> _written = new();

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
        var pending = new List<(FailedPartitionId Id, byte[]? Document)>();
        var current = new HashSet<FailedPartitionId>();

        foreach (var failedPartition in failedPartitions.ResolvedPartitions)
        {
            current.Add(failedPartition.Id);
            if (_written.TryGetValue(failedPartition.Id, out var written) && written.Document is null)
            {
                continue;
            }

            writes.Add(new DeleteOneModel<FailedPartition>(Builders<FailedPartition>.Filter.Eq(_ => _.Id, failedPartition.Id)));
            pending.Add((failedPartition.Id, null));
        }

        foreach (var failedPartition in failedPartitions.Partitions)
        {
            current.Add(failedPartition.Id);
            var document = failedPartition.ToBson();
            if (_written.TryGetValue(failedPartition.Id, out var written) && written.Document?.AsSpan().SequenceEqual(document) == true)
            {
                continue;
            }

            writes.Add(new ReplaceOneModel<FailedPartition>(Builders<FailedPartition>.Filter.Eq(_ => _.Id, failedPartition.Id), failedPartition) { IsUpsert = true });
            pending.Add((failedPartition.Id, document));
        }

        if (writes.Count > 0)
        {
            await _collection.BulkWriteAsync(writes, new BulkWriteOptions { IsOrdered = false }).ConfigureAwait(false);
        }

        foreach (var (id, document) in pending)
        {
            _written[id] = (observerId, document);
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
}
