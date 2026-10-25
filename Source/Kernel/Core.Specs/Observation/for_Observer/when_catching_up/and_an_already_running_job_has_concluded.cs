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
/// A running job that has already reported its work as done cannot own it, so it is not asked to register anything.
/// </summary>
public class and_an_already_running_job_has_concluded : given.an_observer_with_subscription
{
    static readonly JobId _runningJob = JobId.New();
    ICatchUpObserver _job;

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
        await _observer.CaughtUp(_runningJob, 41UL);
        await _observer.CatchUp();
    }

    [Fact] async Task should_not_ask_the_job_to_register_its_partitions() => await _job.DidNotReceive().RegisterPartitionOwnership(Arg.Any<Guid>());
}
