// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_reconciling_subscription;

public class and_a_stopped_catch_up_job_checks_its_subscription : given.an_observer
{
    readonly JobId _jobId = JobId.New();
    bool _canResume;
    bool _establishedDuringResume;

    void Establish()
    {
        var request = new CatchUpObserverRequest(_observerKey, ObserverType.Projection, EventSequenceNumber.First, [EventType.Unknown]);
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(ImmutableList.Create<JobState>(new JobState()
        {
            Id = _jobId,
            Status = JobStatus.Stopped,
            Request = request
        }));
        _jobsManager.Resume(_jobId).Returns(async _ =>
        {
            _establishedDuringResume = await _observer.IsSubscriptionEstablished();

            // CatchUpObserver.CanResume delegates to this check. Keep the observer real and query it inside
            // recovery, before reconcile finishes setup, without activating a second grain in the test silo.
            _canResume = await _observer.IsSubscribed();
            return _canResume;
        });
    }

    Task Because() => _observer.ReconcileSubscription<NullObserverSubscriber>(ObserverType.Projection, [EventType.Unknown], SiloAddress.Zero, false);

    [Fact] async Task should_ask_to_resume_the_stopped_job() => await _jobsManager.Received(1).Resume(_jobId);
    [Fact] void should_allow_the_subscription_check_used_by_catch_up_job_resumption() => _canResume.ShouldBeTrue();
    [Fact] void should_not_require_setup_to_be_complete_before_resuming() => _establishedDuringResume.ShouldBeFalse();
    [Fact] async Task should_establish_the_subscription_after_resuming() => (await _observer.IsSubscriptionEstablished()).ShouldBeTrue();
}
