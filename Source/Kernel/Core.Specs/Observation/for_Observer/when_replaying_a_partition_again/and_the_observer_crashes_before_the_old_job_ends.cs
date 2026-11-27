// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Orleans.Core;
using Orleans.TestKit;
using Orleans.TestKit.Storage;

namespace Cratis.Chronicle.Observation.for_Observer.when_replaying_a_partition_again;

public class and_the_observer_crashes_before_the_old_job_ends : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly Key _partition = "mutated-source";
    readonly List<ReplayObserverPartitionRequest> _starts = [];
    readonly List<string> _addressedJobs = [];
    readonly TaskCompletionSource<JobDrainStatus> _drain = new(TaskCreationOptions.RunContinuationsAsynchronously);
    JobId? _oldJob;
    int _startsWhileDrainIsPending;
    bool _heldAfterOldRefusal;
    bool _holdWasEventuallyReleased;
    SnapshotStorage? _snapshots;
    bool _heldAfterOldCompletion;
    int _repairsAfterCrash;
    int _persistedWaitingBeforeCrash;
    bool _heldDuringReplacement;
    int _storedRevision = 1;
    int _readModelRevision;

    protected override ObserverState PersistedObserverState => _snapshots?.Persisted ?? base.PersistedObserverState;

    protected override void ConfigureStorage(TestKitSilo silo, ObserverState persistedState)
    {
        var snapshots = new SnapshotStorage(persistedState);
        _snapshots = snapshots;
        silo.Options.StorageFactory = type => type == typeof(ObserverState) ? snapshots : null!;
    }

    protected override void ConfigureProbes(TestKitSilo silo)
    {
        var oldJob = Substitute.For<IReplayObserverPartition, IJobDrain>();
        ((IJobDrain)oldJob).StopAndDrain(Arg.Any<TimeSpan>()).Returns(_drain.Task);
        silo.AddProbe<IReplayObserverPartition>(key =>
        {
            _addressedJobs.Add(key.ToString());
            return oldJob;
        });
    }

    async Task Establish()
    {
        await Crash();
        _observer.SetSubscription(subscription);
        _ = _jobsManager.Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(
            Arg.Do<JobId>(job => _oldJob ??= job),
            Arg.Do<ReplayObserverPartitionRequest>(_starts.Add));
        _ = _jobsManager.Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(Arg.Do<ReplayObserverPartitionRequest>(_starts.Add));
    }

    async Task Because()
    {
        await _observer.ReplayPartition(_partition);
        var bufferedRevision = _storedRevision;
        var old = _starts.Single();
        _storedRevision = 2;
        await _observer.ReplayPartition(_partition);
        _storedRevision = 3;
        await _observer.ReplayPartition(_partition);
        _persistedWaitingBeforeCrash = PersistedObserverState.PartitionRepairs?.Single().Waiting.Length ?? 0;

        await Crash();
        _observer.SetSubscription(subscription);

        _repairsAfterCrash = PersistedObserverState.PartitionRepairs?.Count(repair => repair.Retiring is not null) ?? 0;
        await _observer.CheckHealth();
        _startsWhileDrainIsPending = _starts.Count;

        // The old reader can finish its buffered write and report to the new activation.
        _readModelRevision = bufferedRevision;
        await DeliverCompletion(old);
        _heldAfterOldCompletion = PersistedObserverState.ReplayingPartitions.Contains(_partition);
        await _observer.ReleaseRefusedPartitionReplay(_partition, old.HeldBackFrom, old.OwnershipEpoch);
        _heldAfterOldRefusal = PersistedObserverState.ReplayingPartitions.Contains(_partition);
        _drain.SetResult(JobDrainStatus.Drained);
        await _observer.CheckHealth();
        await DeliverCompletion(old);
        await _observer.ReleaseRefusedPartitionReplay(_partition, old.HeldBackFrom, old.OwnershipEpoch);
        _heldDuringReplacement = PersistedObserverState.ReplayingPartitions.Contains(_partition);
        foreach (var replacement in _starts.Skip(1).ToArray())
        {
            _readModelRevision = _storedRevision;
            await DeliverCompletion(replacement);
        }
        await _observer.CheckHealth();
        await DeliverCompletion(old);
        await _observer.ReleaseRefusedPartitionReplay(_partition, old.HeldBackFrom, old.OwnershipEpoch);
        _holdWasEventuallyReleased = !PersistedObserverState.ReplayingPartitions.Contains(_partition);
    }

    [Fact] void should_persist_one_equivalent_waiting_repair_before_ack() => _persistedWaitingBeforeCrash.ShouldEqual(1);
    [Fact] void should_restore_the_owed_repair_and_retiring_job() => _repairsAfterCrash.ShouldEqual(1);
    [Fact] void should_address_the_exact_old_job() => _addressedJobs.Single().ShouldContain(_oldJob!.Value.ToString());
    [Fact] void should_not_replace_a_writer_before_the_exact_drain_answers() => _startsWhileDrainIsPending.ShouldEqual(1);
    [Fact] void should_not_release_the_partition_on_the_old_refusal() => _heldAfterOldRefusal.ShouldBeTrue();
    [Fact] void should_release_the_hold_after_the_fresh_repair() => _holdWasEventuallyReleased.ShouldBeTrue();
    [Fact] void should_not_release_the_partition_on_the_old_completion() => _heldAfterOldCompletion.ShouldBeTrue();
    [Fact] void should_ignore_both_stale_reports_during_the_fresh_run() => _heldDuringReplacement.ShouldBeTrue();
    [Fact] void should_run_one_fresh_repair() => _starts.Count.ShouldEqual(2);
    [Fact] void should_finish_with_the_latest_revision() => _readModelRevision.ShouldEqual(3);

    Task DeliverCompletion(ReplayObserverPartitionRequest request) =>
        _observer.PartitionReplayed(request.Key, EventSequenceNumber.First, EventSequenceNumber.First, [], request.OwnershipEpoch);

    sealed class SnapshotStorage(ObserverState initial) : IStorage<ObserverState>, IStorageStats
    {
        public ObserverState Persisted { get; private set; } = Copy(initial);
        public ObserverState State { get; set; } = Copy(initial);
        public TestStorageStats Stats { get; } = new() { Reads = -1 };
        public string Etag => string.Empty;
        public bool RecordExists { get; private set; } = true;

        public Task ReadStateAsync()
        {
            State = Copy(Persisted);
            Stats.Reads++;
            return Task.CompletedTask;
        }

        public Task WriteStateAsync()
        {
            Persisted = Copy(State);
            RecordExists = true;
            Stats.Writes++;
            return Task.CompletedTask;
        }

        public Task ClearStateAsync()
        {
            Persisted = new();
            RecordExists = false;
            Stats.Clears++;
            return Task.CompletedTask;
        }

        static ObserverState Copy(ObserverState state) => state with
        {
            ReplayingPartitions = state.ReplayingPartitions.ToHashSet(),
            CatchingUpPartitions = state.CatchingUpPartitions.ToHashSet(),
            InFlightPartitions = state.InFlightPartitions.ToHashSet(),
            PartitionClaims = state.PartitionClaims?.ToArray(),
            DeliveryFences = state.DeliveryFences?.ToArray(),
            HandledEventCountPerEventType = state.HandledEventCountPerEventType.ToDictionary()
        };
    }
}
