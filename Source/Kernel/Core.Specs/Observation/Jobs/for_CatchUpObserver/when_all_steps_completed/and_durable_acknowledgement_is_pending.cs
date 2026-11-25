// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_all_steps_completed;

public class and_durable_acknowledgement_is_pending : given.a_catch_up_observer_job
{
    readonly TaskCompletionSource _accepted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _waited;

    async Task Establish() => await StartAndLetTheStepsCome();

    async Task Because()
    {
        _observer.CaughtUp(Arg.Any<JobId>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>()).Returns(_accepted.Task);
        var completing = _job.CompleteForTesting();
        try
        {
            _waited = !completing.IsCompleted;
        }
        finally
        {
            _accepted.TrySetResult();
        }
        await completing.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_not_finish_while_the_observer_has_not_accepted_durably() => _waited.ShouldBeTrue();
}
