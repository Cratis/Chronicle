// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_verifying_live_subscription_after_a_silo_departure;

/// <summary>
/// The observer holds a partition for catch-up, but the catch-up job that owned it ran on the silo that left and is
/// gone. Nothing would report back, so the held partition refuses every live event until the watchdog - a minute apart -
/// notices. The pass that follows the departure routes the observer instead, which starts the catch-up again.
/// </summary>
public class and_the_catch_up_job_went_down_with_the_silo : given.an_observing_observer
{
    async Task Establish()
    {
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        await _observer.RegisterCatchingUpPartitions(["partition"]);
        _appendedEventsQueues.ClearReceivedCalls();
    }

    async Task Because() => await _observer.VerifyLiveSubscriptionAfterDeparture();

    [Fact] void should_route_the_observer_again() => ShouldHaveResubscribed();
    [Fact] async Task should_be_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
}
