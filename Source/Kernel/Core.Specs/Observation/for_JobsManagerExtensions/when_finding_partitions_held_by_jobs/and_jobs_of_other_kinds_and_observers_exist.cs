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

public class and_jobs_of_other_kinds_and_observers_exist : given.a_jobs_manager
{
    readonly Key _matching = new("matching", ArrayIndexers.NoIndexers);
    readonly Key _stopped = new("stopped", ArrayIndexers.NoIndexers);
    readonly Key _removing = new("removing", ArrayIndexers.NoIndexers);
    JobState[] _jobs;
    ISet<Key> _held;

    void Establish()
    {
        var other = _observerKey with { ObserverId = "another-observer" };
        _jobs =
        [
            PartitionJob(_matching, JobStatus.Running, _observerKey),
            PartitionJob(_stopped, JobStatus.Stopped, _observerKey),
            PartitionJob(_removing, JobStatus.Removing, _observerKey),
            PartitionJob(new Key("other-observer", ArrayIndexers.NoIndexers), JobStatus.Running, other),
            PartitionJob(new Key("completed", ArrayIndexers.NoIndexers), JobStatus.CompletedSuccessfully, _observerKey),
            AJob(JobStatus.Running),
            new JobState
            {
                Id = JobId.New(),
                Status = JobStatus.Running,
                Request = new ReplayObserverPartitionRequest(_observerKey, ObserverType.Reactor, new Key("replay", ArrayIndexers.NoIndexers), EventSequenceNumber.First, 10UL, [])
            }
        ];
    }

    void Because() => _held = _jobs.PartitionsHeldBy<CatchUpObserverPartitionRequest>(_observerKey, DateTimeOffset.UtcNow);

    [Fact] void should_only_hold_partitions_of_outstanding_matching_jobs() => _held.ShouldContainOnly(_matching, _stopped, _removing);

    static JobState PartitionJob(Key key, JobStatus status, ObserverKey observer) => new()
    {
        Id = JobId.New(),
        Status = status,
        Request = new CatchUpObserverPartitionRequest(observer, ObserverType.Reactor, key, EventSequenceNumber.First, [])
    };
}
