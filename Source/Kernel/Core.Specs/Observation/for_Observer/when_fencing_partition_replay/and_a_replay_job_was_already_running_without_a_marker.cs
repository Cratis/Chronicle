// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_a_replay_job_was_already_running_without_a_marker : given.an_observer_with_partition_replay_jobs
{
    bool _fenced;

    void Establish() => _jobs = ImmutableList.Create(
        ReplayJob(_firstJob, _partition),
        ReplayJob(_secondJob, _otherPartition, JobStatus.CompletedSuccessfully),
        ReplayJob(JobId.New(), _otherPartition, observerKey: new ObserverKey("other-observer", "store", "namespace", EventSequenceId.Log)));

    async Task Because() => _fenced = await _observer.IsPartitionFenced(_partition, _secondJob);

    [Fact] void should_fence_delivery() => _fenced.ShouldBeTrue();
    [Fact] async Task should_not_fence_unrelated_or_finished_work() => (await _observer.IsPartitionFenced(_otherPartition, _secondJob)).ShouldBeFalse();
    [Fact] async Task should_exclude_the_requesting_job() => (await _observer.IsPartitionFenced(_partition, _firstJob)).ShouldBeFalse();
}
