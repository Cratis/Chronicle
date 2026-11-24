// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverOperatorCommandStep;

public class when_giving_a_replay_to_an_observer_that_never_answers : given.an_observer_to_give_commands_to
{
    Exception _error;

    void Establish() => _observer.Replay().Returns(_ => Task.FromException<JobId>(new TimeoutException()));

    async Task Because() => _error = await Catch.Exception(() => Give(ObserverOperatorCommand.Replay));

    [Fact] void should_give_up_after_the_most_attempts() => _observer.Received(3).Replay();
    [Fact] void should_fail_the_step_with_the_timeout() => _error.ShouldBeOfExactType<TimeoutException>();
}
