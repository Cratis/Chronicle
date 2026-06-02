// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_unsubscribing_and_there_are_ongoing_jobs : given.an_observer_with_subscription
{
    JobId _catchUpJobId;
    JobId _replayJobId;

    void Establish()
    {
        _catchUpJobId = Guid.NewGuid();
        _replayJobId = Guid.NewGuid();

        var catchUpJob = new JobState
        {
            Id = _catchUpJobId,
            Status = JobStatus.Running,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [EventType.Unknown])
        };

        var replayJob = new JobState
        {
            Id = _replayJobId,
            Status = JobStatus.Running,
            Request = new ReplayObserverRequest(_observerKey, ObserverType.Reactor, [EventType.Unknown])
        };

        _jobsManager.GetAllJobs().Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(catchUpJob, replayJob)));
    }

    Task Because() => _observer.Unsubscribe();

    [Fact] void should_stop_the_catch_up_job() => _jobsManager.Received(1).Stop(_catchUpJobId);
    [Fact] void should_not_stop_the_replay_job() => _jobsManager.DidNotReceive().Stop(_replayJobId);
}
