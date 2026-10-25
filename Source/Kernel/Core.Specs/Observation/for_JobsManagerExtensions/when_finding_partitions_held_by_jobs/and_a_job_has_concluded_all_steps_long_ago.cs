// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Properties;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_finding_partitions_held_by_jobs;

public class and_a_job_has_concluded_all_steps_long_ago : given.a_jobs_manager
{
    readonly DateTimeOffset _now = DateTimeOffset.UtcNow;
    JobState[] _jobs;
    ISet<Key> _held;

    void Establish() => _jobs =
    [
        new JobState
        {
            Id = JobId.New(),
            Status = JobStatus.Running,
            Request = new CatchUpObserverPartitionRequest(_observerKey, ObserverType.Reactor, new Key("stale", ArrayIndexers.NoIndexers), EventSequenceNumber.First, []),
            Progress = new() { TotalSteps = 3, SuccessfulSteps = 3 },
            StatusChanges = [new JobStatusChanged { Status = JobStatus.Running, Occurred = _now - JobLiveness.StaleAfter - TimeSpan.FromSeconds(1) }]
        }
    ];

    void Because() => _held = _jobs.PartitionsHeldBy<CatchUpObserverPartitionRequest>(_observerKey, _now);

    [Fact] void should_not_hold_the_partition_for_a_stale_job() => _held.ShouldBeEmpty();
}
