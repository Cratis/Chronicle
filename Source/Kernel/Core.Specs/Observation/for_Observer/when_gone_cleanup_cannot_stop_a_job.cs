// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_gone_cleanup_cannot_stop_a_job : given.a_gone_retired_webhook
{
    JobId _jobId;
    Guid _retirement;
    Exception _error;

    async Task Establish()
    {
        _retirement = _stateStorage.State.WebhookRetirement.Id;
        _jobId = JobId.New();
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult<IImmutableList<JobState>>(
            ImmutableList.Create(new JobState { Id = _jobId, Status = JobStatus.Running, Request = new ReplayObserverRequest(_observerKey, ObserverType.External, ["Event1", "Event2"]) })));
        await Crash();
        _jobsManager.Stop(_jobId).Returns(Result<StopJobError>.Failed(StopJobError.CouldNotPersistStop));
    }

    async Task Because()
    {
        _error = await Cratis.Specifications.Catch.Exception(_observer.RunWatchdogAsync);
        _jobsManager.Stop(_jobId).Returns(Result<StopJobError>.Success());
        await Crash();
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_surface_the_cleanup_failure() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_retry_stopping_the_retained_job() => _jobsManager.Received(2).Stop(_jobId);
    [Fact] void should_keep_the_same_terminal_incident() => _stateStorage.State.WebhookRetirement.Id.ShouldEqual(_retirement);
    [Fact] void should_preserve_the_failed_event() => _failedPartitionsState.Partitions.Single().EffectiveRecoveryCursor.Position.ShouldEqual((EventSequenceNumber)42UL);
}
