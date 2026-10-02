// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_removing;

public class and_the_observer_has_jobs : given.an_observer
{
    JobId _jobId;
    JobId _otherSequenceJobId;
    JobId _otherObserverJobId;

    void Establish()
    {
        _jobId = Guid.NewGuid();
        _otherSequenceJobId = Guid.NewGuid();
        _otherObserverJobId = Guid.NewGuid();
        _jobsManager.GetAllJobs().Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(
            new JobState
            {
                Id = _jobId,
                Request = new ReplayObserverRequest(_observerKey, ObserverType.Reactor, [])
            },
            new JobState
            {
                Id = _otherSequenceJobId,
                Request = new ReplayObserverRequest(_observerKey with { EventSequenceId = new EventSequenceId("other-sequence") }, ObserverType.Reactor, [])
            },
            new JobState
            {
                Id = _otherObserverJobId,
                Request = new ReplayObserverRequest(_observerKey with { ObserverId = "other-observer" }, ObserverType.Reactor, [])
            })));
    }

    Task Because() => _observer.Remove();

    [Fact] async Task should_delete_the_observers_job() => await _jobsManager.Received(1).Delete(_jobId);
    [Fact] async Task should_delete_the_observers_job_on_another_sequence() => await _jobsManager.Received(1).Delete(_otherSequenceJobId);
    [Fact] async Task should_leave_the_other_observers_job_alone() => await _jobsManager.DidNotReceive().Delete(_otherObserverJobId);
    [Fact] async Task should_only_delete_the_observers_jobs() => await _jobsManager.Received(2).Delete(Arg.Any<JobId>());
}
