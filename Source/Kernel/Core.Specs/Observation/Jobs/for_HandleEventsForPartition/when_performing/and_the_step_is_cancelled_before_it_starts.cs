// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_the_step_is_cancelled_before_it_starts : given.a_performing_job_step
{
    Catch<JobStepResult> _result;

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState, new CancellationToken(true));

    [Fact] void should_end_the_step_as_cancelled() => _result.Match(result => result.TryGetError(out var error) && error.Cancelled, _ => false).ShouldBeTrue();
    [Fact] void should_not_read_any_events() => _eventTypesStorage.DidNotReceive().GetFor(Arg.Any<IEnumerable<EventType>>());
}
