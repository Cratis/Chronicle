// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.for_Observer.given;
using Cratis.Chronicle.Storage.Observation;
using Orleans.Core;
using Orleans.TestKit.Storage;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_replayed.given;

public class an_observer_with_replaying_partition : an_observer
{
    protected static Key _partition;
    protected static EventSequenceNumber _lastHandledEventSequenceNumber;
    protected static EventSequenceNumber _nextEventSequenceNumber;
    readonly IStorage<ObserverState> _persistedStorage = Substitute.For<IStorage<ObserverState>, IStorageStats>();
    readonly List<ObserverState> _persistedSnapshots = [];

    public an_observer_with_replaying_partition()
    {
        _persistedStorage.State = ObserverState.Empty;
        var stats = new TestStorageStats { Reads = -1 };
        ((IStorageStats)_persistedStorage).Stats.Returns(stats);
        _persistedStorage.ReadStateAsync().Returns(_ =>
        {
            stats.Reads++;
            return Task.CompletedTask;
        });
        _persistedStorage.WriteStateAsync().Returns(_ =>
        {
            stats.Writes++;
            _persistedStorage.RecordExists.Returns(true);
            _persistedSnapshots.Add(_persistedStorage.State with
            {
                ReplayingPartitions = new HashSet<Key>(_persistedStorage.State.ReplayingPartitions),
                CatchingUpPartitions = new HashSet<Key>(_persistedStorage.State.CatchingUpPartitions),
                PartitionReplayCompletions = _persistedStorage.State.PartitionReplayCompletions?.ToArray()
            });
            return Task.CompletedTask;
        });
        _silo.Options.StorageFactory = type => type == typeof(ObserverState) ? _persistedStorage : null!;
    }

    protected bool ReplayCompletionWasPersisted
    {
        get
        {
            var recorded = _persistedSnapshots.FindIndex(state => state.ReplayingPartitions.Contains(_partition)
                && (state.PartitionReplayCompletions?.Any(completion => completion.Partition == _partition) ?? false));
            return recorded >= 0 && _persistedSnapshots.Skip(recorded + 1).Any(state => !state.ReplayingPartitions.Contains(_partition)
                && !(state.PartitionReplayCompletions?.Any(completion => completion.Partition == _partition) ?? false));
        }
    }

    void Establish()
    {
        _partition = "some-partition";
        _lastHandledEventSequenceNumber = 42UL;
        _nextEventSequenceNumber = 45UL;
        _stateStorage.State = _stateStorage.State with
        {
            LastHandledEventSequenceNumber = _lastHandledEventSequenceNumber,
            NextEventSequenceNumber = _nextEventSequenceNumber,
            ReplayingPartitions = new HashSet<Key>([_partition])
        };
    }

    protected void CheckStartedCatchupJob(EventSequenceNumber lastHandled) => CheckStartedCatchupJob(lastHandled, _partition);
}
