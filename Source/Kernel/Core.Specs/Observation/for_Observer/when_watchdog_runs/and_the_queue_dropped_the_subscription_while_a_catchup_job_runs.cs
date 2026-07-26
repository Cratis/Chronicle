// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

/// <summary>
/// A spill whose catch-up trigger did succeed leaves exactly the same observer state - subscribed, active and behind
/// with the queue no longer holding the subscription - while the catch-up job is already driving recovery. Rescuing
/// there would start a second catch-up for the same range, so the running job is what the watchdog defers to.
/// </summary>
public class and_the_queue_dropped_the_subscription_while_a_catchup_job_runs : given.an_observer_behind_on_a_relevant_event
{
    void Establish()
    {
        _appendedEventsQueues.IsSubscribed(_observerKey).Returns(false);

        var runningJob = new JobState
        {
            Id = JobId.New(),
            Status = JobStatus.Running,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, _nextSequenceNumber, [])
        };

        _jobsManager
            .GetJobsOfType<ICatchUpObserver, CatchUpObserverRequest>()
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty.Add(runningJob)));
    }

    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact] void should_not_resubscribe_the_observer() => ShouldNotHaveResubscribed();

    [Fact] void should_not_start_a_second_catchup_job() => ShouldNotHaveStartedCatchup();
}
