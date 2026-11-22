// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_FastForward;

public class when_handling : given.a_grain_factory_accepting_operator_commands
{
    readonly TaskCompletionSource<FastForwardResult> _grainCall = new();
    FastForwardResult _result;

    void Establish() => _observer.FastForward().Returns(_grainCall.Task);

    async Task Because()
    {
        // Fails with a timeout, not by hanging, when the command waits for the observer.
        _result = await new FastForward("some-event-store", "some-namespace", "some-observer", string.Empty)
            .Handle(_grainFactory, _storage)
            .WaitAsync(TimeSpan.FromSeconds(5));
    }

    void Destroy() => _grainCall.TrySetResult(new(FastForwardOutcome.FastForwarded, 0UL, 0UL));

    [Fact] void should_accept_the_fast_forward_as_a_job_for_the_observer_of_the_stored_sequence() => _jobsManager.Received(1).Start<IObserverOperatorCommandJob, ObserverOperatorCommandRequest>(
        new ObserverOperatorCommandRequest(_key, ObserverOperatorCommand.FastForward));
    [Fact] void should_report_that_it_was_accepted() => _result.Outcome.ShouldEqual(FastForwardOutcome.Accepted);
    [Fact] void should_not_call_the_observer() => _observer.DidNotReceive().FastForward();
}
