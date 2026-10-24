// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an <see cref="ISink"/> that holds the state of exactly one key in memory while a partition is rebuilt
/// out of sight of the live read model.
/// </summary>
/// <param name="partition">The <see cref="Key"/> of the only partition the sink accepts.</param>
/// <remarks>
/// The sink never persists anything: the caller decides what to keep through <see cref="State"/>, <see cref="IsRemoved"/>
/// and <see cref="Watermark"/>. Any other key is a rebuild that left its partition, which is refused rather than
/// written, so it can never reach the live read model.
/// </remarks>
internal sealed class PartitionRebuildSink(Key partition) : ISink
{
    /// <inheritdoc/>
    public SinkTypeId TypeId => SinkTypeId.None;

    /// <summary>
    /// Gets the state rebuilt so far, or null when nothing was written or the instance was removed.
    /// </summary>
    public ExpandoObject? State { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the last write removed the instance.
    /// </summary>
    public bool IsRemoved { get; private set; }

    /// <summary>
    /// Gets the sequence number of the last event that was applied.
    /// </summary>
    public EventSequenceNumber Watermark { get; private set; } = EventSequenceNumber.Unavailable;

    /// <inheritdoc/>
    public Task<ExpandoObject?> FindOrDefault(Key key)
    {
        EnsureOwn(key);
        return Task.FromResult(State);
    }

    /// <inheritdoc/>
    public Task<ExpandoObject?> FindInPrimaryOrDefault(Key key) => FindOrDefault(key);

    /// <inheritdoc/>
    /// <remarks>A partition rebuild resolves its key from the partition, so there is no other instance to find.</remarks>
    public Task<Option<Key>> TryFindRootKeyByChildValue(PropertyPath childPropertyPath, object childValue) =>
        Task.FromResult(Option<Key>.None());

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber)
    {
        EnsureOwn(key);

        if (changeset.HasBeenRemoved())
        {
            State = null;
            IsRemoved = true;
        }
        else
        {
            var current = (State ?? changeset.InitialState).Clone();
            State = InMemoryChanges.Apply(key, changeset.Changes, current);
            IsRemoved = false;
        }

        Watermark = eventSequenceNumber;
        return Task.FromResult<IEnumerable<FailedPartition>>([]);
    }

    /// <inheritdoc/>
    public Task BeginBulk() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> EndBulk() => Task.FromResult<IEnumerable<FailedPartition>>([]);

    /// <inheritdoc/>
    public Task BeginReplay(ReplayContext context) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task ResumeReplay(ReplayContext context) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> EndReplay(ReplayContext context) => Task.FromResult<IEnumerable<FailedPartition>>([]);

    /// <inheritdoc/>
    public Task Remove(ReadModelContainerName containerName) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task PrepareInitialRun() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task EnsureIndexes() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<ReadModelInstances> GetInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50) =>
        Task.FromResult(State is null || skip > 0 || take < 1
            ? new ReadModelInstances([], State is null ? 0 : 1)
            : new ReadModelInstances([State], 1));

    /// <inheritdoc/>
    public IObservable<IEnumerable<ExpandoObject>> ObserveInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50) =>
        Observable.Empty<IEnumerable<ExpandoObject>>();

    void EnsureOwn(Key key)
    {
        if (!Equals(key.Value, partition.Value))
        {
            throw new PartitionRebuildEscapedPartition(partition, key);
        }
    }
}
