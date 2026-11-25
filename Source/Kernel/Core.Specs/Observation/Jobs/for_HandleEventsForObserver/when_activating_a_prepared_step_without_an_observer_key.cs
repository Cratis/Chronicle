// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver;

public class when_activating_a_prepared_step_without_an_observer_key : given.a_performing_job_step
{
    Catch<JobStepResult> _result;

    void Establish()
    {
        _performState.IsPrepared = true;
        _performState.ObserverKey = ObserverKey.NotSet;
        _observer.ClearReceivedCalls();
        _observer.GetSubscription().Returns<Task<ObserverSubscription>>(_ => throw new InvalidOperationException("The missing observer must not be resolved"));
    }

    async Task Because()
    {
        await _jobStep.OnActivateAsync(CancellationToken.None);
        _result = await _jobStep.InvokePerformStep(_performState);
    }

    [Fact] void should_not_resolve_the_missing_observer() => _observer.DidNotReceive().GetSubscription();
    [Fact] void should_return_a_terminal_failure() => _result.Match(result => result.TryGetError(out var error) && !error.Cancelled, _ => false).ShouldBeTrue();
    [Fact] void should_explain_the_missing_state() => _result.Match(result => result.TryGetError(out var error) && error.ErrorMessages.Contains("The prepared job step state was missing its observer key"), _ => false).ShouldBeTrue();
    [Fact] void should_not_read_any_events() => _eventCursor.DidNotReceive().MoveNext();
}
