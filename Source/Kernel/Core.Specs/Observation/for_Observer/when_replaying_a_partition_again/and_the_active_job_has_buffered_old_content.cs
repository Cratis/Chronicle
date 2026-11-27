// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_replaying_a_partition_again;

public class and_the_active_job_has_buffered_old_content : given.an_observer
{
    static readonly Key _partition = "mutated-source";
    readonly List<ReplayObserverPartitionRequest> _starts = [];
    int _storedRevision = 1;
    int _readModelRevision;
    int _startsBeforeRelease;
    int _startsForAnotherPartition;

    void Establish() => _jobsManager.Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(Arg.Any<JobId>(), Arg.Do<ReplayObserverPartitionRequest>(_starts.Add));

    async Task Because()
    {
        await _observer.ReplayPartition(_partition);
        var bufferedRevision = _storedRevision;
        var first = _starts.Single();

        _storedRevision = 2;
        await _observer.ReplayPartition(_partition);
        _storedRevision = 3;
        await _observer.ReplayPartition(_partition);
        _startsBeforeRelease = _starts.Count;

        Key other = "independent-source";
        await _observer.ReplayPartition(other);
        _startsForAnotherPartition = _starts.Count(_ => _.Key == other);

        // An overlapping job can finish before the first reader delivers its buffered event.
        foreach (var overlapping in _starts.Where(_ => _.Key == _partition).Skip(1).ToArray())
        {
            await DeliverAndComplete(overlapping, _storedRevision);
        }

        var beforeCompletion = _starts.Count;
        await DeliverAndComplete(first, bufferedRevision);
        foreach (var followUp in _starts.Skip(beforeCompletion).ToArray())
        {
            await DeliverAndComplete(followUp, _storedRevision);
        }
    }

    async Task DeliverAndComplete(ReplayObserverPartitionRequest request, int revision)
    {
        _readModelRevision = revision;
        await _observer.PartitionReplayed(request.Key, EventSequenceNumber.First, EventSequenceNumber.First, [], request.OwnershipEpoch);
    }

    [Fact] void should_not_start_another_rebuild_before_the_active_job_releases_ownership() => _startsBeforeRelease.ShouldEqual(1);
    [Fact] void should_not_block_an_independent_partition() => _startsForAnotherPartition.ShouldEqual(1);
    [Fact] void should_coalesce_queued_repairs_into_one_follow_up() => _starts.Count(_ => _.Key == _partition).ShouldEqual(2);
    [Fact] void should_finish_with_the_latest_revision() => _readModelRevision.ShouldEqual(3);
}
