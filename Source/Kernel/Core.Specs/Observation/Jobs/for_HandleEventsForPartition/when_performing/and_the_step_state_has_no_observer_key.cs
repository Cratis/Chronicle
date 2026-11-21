// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_the_step_state_has_no_observer_key : given.a_performing_job_step
{
    Catch<JobStepResult> _result;

    void Establish() => _performState.ObserverKey = ObserverKey.NotSet;

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_fail_without_cancelling() => _result.Match(result => result.TryGetError(out var error) && !error.Cancelled, _ => false).ShouldBeTrue();
    [Fact] void should_not_ask_the_observer_for_its_subscription() => _observer.DidNotReceive().GetSubscription();
}
