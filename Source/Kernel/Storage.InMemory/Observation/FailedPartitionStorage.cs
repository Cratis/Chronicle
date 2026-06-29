// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation;

/// <summary>
/// Represents an in-memory implementation of <see cref="IFailedPartitionsStorage"/>.
/// </summary>
public sealed class FailedPartitionStorage : IFailedPartitionsStorage
{
    readonly ConcurrentDictionary<FailedPartitionId, FailedPartition> _partitions = new();
    readonly ReplaySubject<IEnumerable<FailedPartition>> _allSubject = new(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="FailedPartitionStorage"/> class.
    /// </summary>
    public FailedPartitionStorage() => _allSubject.OnNext(Snapshot());

    /// <inheritdoc/>
    public ISubject<IEnumerable<FailedPartition>> ObserveAllFor(ObserverId? observerId = default)
    {
        if (observerId is null)
        {
            return _allSubject;
        }

        var filtered = new ReplaySubject<IEnumerable<FailedPartition>>(1);
        filtered.OnNext(SnapshotFor(observerId));
        _allSubject.Subscribe(_ => filtered.OnNext(SnapshotFor(observerId)));
        return filtered;
    }

    /// <inheritdoc/>
    public Task Save(ObserverId observerId, FailedPartitions failedPartitions)
    {
        foreach (var resolved in failedPartitions.ResolvedPartitions)
        {
            _partitions.TryRemove(resolved.Id, out _);
        }

        foreach (var partition in failedPartitions.Partitions)
        {
            _partitions[partition.Id] = partition;
        }

        _allSubject.OnNext(Snapshot());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<FailedPartitions> GetFor(ObserverId? observerId)
    {
        var partitions = observerId is null
            ? Snapshot()
            : SnapshotFor(observerId);

        return Task.FromResult(new FailedPartitions { Partitions = partitions });
    }

    /// <inheritdoc/>
    public Task<FailedPartitions> GetFor(IEnumerable<ObserverId> observerIds)
    {
        var ids = observerIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return Task.FromResult(new FailedPartitions());
        }

        var partitions = _partitions.Values.Where(_ => ids.Contains(_.ObserverId)).ToArray();
        return Task.FromResult(new FailedPartitions { Partitions = partitions });
    }

    IEnumerable<FailedPartition> Snapshot() => _partitions.Values.ToArray();

    IEnumerable<FailedPartition> SnapshotFor(ObserverId observerId) =>
        _partitions.Values.Where(_ => _.ObserverId == observerId).ToArray();
}
