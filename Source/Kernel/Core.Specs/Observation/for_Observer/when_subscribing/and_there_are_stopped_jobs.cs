// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_there_are_stopped_jobs : given.an_observer
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
            Status = JobStatus.Stopped,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [EventType.Unknown])
        };

        var replayJob = new JobState
        {
            Id = _replayJobId,
            Status = JobStatus.Stopped,
            Request = new ReplayObserverRequest(_observerKey, ObserverType.Reactor, [EventType.Unknown])
        };

        _jobsManager.GetAllJobs().Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(catchUpJob, replayJob)));
    }

    Task Because() => _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);

    [Fact] void should_resume_the_stopped_catch_up_job() => _jobsManager.Received(1).Resume(_catchUpJobId);
    [Fact] void should_not_resume_the_stopped_replay_job() => _jobsManager.DidNotReceive().Resume(_replayJobId);
}
