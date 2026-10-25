// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.when_catching_up;

/// <summary>
/// A job that is already running may have been resumed before the observer knew about it, and routing releases the holds a job of its kind registered. The observer asks the job to register what it owns - without waiting for it, as the job may be waiting for the observer - and keeps catch-up preparing until it has.
/// </summary>
public class and_a_catch_up_job_is_already_running : given.an_observer_with_subscription
{
    static readonly JobId _runningJob = JobId.New();
    ICatchUpObserver _job;
    bool _isPreparingCatchup;

    void Establish()
    {
        _job = Substitute.For<ICatchUpObserver>();
        _silo.AddProbe(_ => _job);
        _jobsManager
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(Task.FromResult<IImmutableList<JobState>>(
                ImmutableList.Create(new JobState
                {
                    Id = _runningJob,
                    Status = JobStatus.Running,
                    Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [])
                })));
    }

    async Task Because()
    {
        await _observer.CatchUp();
        _isPreparingCatchup = await _observer.IsPreparingCatchup();
    }

    [Fact] async Task should_ask_the_running_job_to_register_its_partitions() => await _job.Received(1).RegisterPartitionOwnership();
    [Fact] void should_keep_preparing_catch_up() => _isPreparingCatchup.ShouldBeTrue();
    [Fact] async Task should_not_resume_or_start_a_job() => await _jobsManager.DidNotReceive().Resume(Arg.Any<JobId>());
}
