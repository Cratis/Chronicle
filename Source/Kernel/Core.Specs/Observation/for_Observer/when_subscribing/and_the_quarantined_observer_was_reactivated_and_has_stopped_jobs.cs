// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

/// <summary>
/// Subscribing a quarantined observer after its grain was reactivated must resume the jobs that were stopped
/// while it was away, the same as subscribing any other observer.
/// </summary>
public class and_the_quarantined_observer_was_reactivated_and_has_stopped_jobs : given.a_reactivated_quarantined_observer
{
    JobId _catchUpJobId;

    void Establish()
    {
        _catchUpJobId = Guid.NewGuid();
        var catchUpJob = new JobState
        {
            Id = _catchUpJobId,
            Status = JobStatus.Stopped,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [EventType.Unknown])
        };

        _jobsManager.GetAllJobs().Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(catchUpJob)));
    }

    Task Because() => _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);

    [Fact] async Task should_not_be_quarantined() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
    [Fact] void should_resume_the_stopped_catch_up_job() => _jobsManager.Received(1).Resume(_catchUpJobId);
}
