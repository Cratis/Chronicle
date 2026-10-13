// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing;

public class and_state_requires_the_activation_context : given.a_performing_job_step
{
    Catch<JobStepResult> _result;
    Exception? _error;

    void Establish() => _jobStep.GuardStateAccess = true;

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(async () => _result = await _jobStep.InvokePerformStep(_performState));

    [Fact] void should_not_access_persistent_state_from_the_worker() => _error.ShouldBeNull();
    [Fact] void should_complete_successfully() => _result.Match(result => result.TryGetError(out _), _ => true).ShouldBeFalse();
    [Fact] void should_confirm_the_completed_range_on_the_grain() => _performState.LastSuccessfullyHandledEventSequenceNumber.Value.ShouldEqual(3UL);
}
