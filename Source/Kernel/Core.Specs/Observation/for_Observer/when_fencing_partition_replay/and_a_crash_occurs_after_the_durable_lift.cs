// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;
using Orleans.Core;
using Orleans.TestKit.Storage;
using JobError = Cratis.Orleans.Storage.Jobs.JobError;

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_a_crash_occurs_after_the_durable_lift : given.an_observer_with_partition_replay_jobs
{
    readonly IStorage<ObserverState> _durableStorage = Substitute.For<IStorage<ObserverState>, IStorageStats>();
    ObserverState _persisted = ObserverState.Empty;
    bool _interruptRemoval;
    bool _liftWasDurable;

    public and_a_crash_occurs_after_the_durable_lift()
    {
        _durableStorage.State = ObserverState.Empty;
        ((IStorageStats)_durableStorage).Stats.Returns(new TestStorageStats());
        _silo.Options.StorageFactory = type => type == typeof(ObserverState) ? _durableStorage : null!;
        _durableStorage.WriteStateAsync().Returns(_ => Persist());
        _durableStorage.ReadStateAsync().Returns(_ =>
        {
            _durableStorage.State = _persisted;
            return Task.CompletedTask;
        });
    }

    void Establish()
    {
        _stateStorage.State.ReplayingPartitions.Add(_partition);
        EventSequenceHasNextEvent(48UL);
        _jobStorage.GetJob(_firstJob).Returns(Task.FromResult<Catch<JobState, JobError>>(JobError.NotFound));
        _interruptRemoval = true;
    }

    async Task Because()
    {
        await _observer.PartitionReplayed(_partition, 42UL, 48UL, [], _firstJob);
        _liftWasDurable = !_persisted.ReplayingPartitions.Contains(_partition)
            && _persisted.PartitionReplayCompletions!.Single().HandedOff;
        _interruptRemoval = false;
        _jobsManager.ClearReceivedCalls();
        await Crash();
    }

    bool ShouldInterruptCompletionRemoval => _interruptRemoval && _persisted.PartitionReplayCompletions!.Any(record => record.HandedOff)
        && !_durableStorage.State.PartitionReplayCompletions!.Any();

    Task Persist()
    {
        if (ShouldInterruptCompletionRemoval)
        {
            return Task.FromException(new InvalidOperationException("activation lost before completion removal"));
        }
        _persisted = _durableStorage.State with
        {
            ReplayingPartitions = new HashSet<Key>(_durableStorage.State.ReplayingPartitions),
            CatchingUpPartitions = new HashSet<Key>(_durableStorage.State.CatchingUpPartitions),
            PartitionReplayCompletions = _durableStorage.State.PartitionReplayCompletions!.ToArray()
        };
        return Task.CompletedTask;
    }

    [Fact] void should_reach_the_durable_lift_before_the_crash() => _liftWasDurable.ShouldBeTrue();
    [Fact] void should_not_restore_the_replay_fence() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_finish_removing_the_completion() => _stateStorage.State.PartitionReplayCompletions!.ShouldBeEmpty();
    [Fact] void should_not_repeat_the_catchup_handoff() => CheckDidNotStartCatchupJob();
}
