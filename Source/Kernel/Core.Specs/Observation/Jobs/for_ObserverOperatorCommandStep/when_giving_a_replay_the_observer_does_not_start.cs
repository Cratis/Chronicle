// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverOperatorCommandStep;

/// <summary>
/// An observer that is quarantined, has no client or cannot be replayed starts no replay; the job fails and says so.
/// </summary>
public class when_giving_a_replay_the_observer_does_not_start : given.an_observer_to_give_commands_to
{
    Exception _error;

    void Establish() => _observer.Replay().Returns(JobId.NotSet);

    async Task Because() => _error = await Catch.Exception(() => Give(ObserverOperatorCommand.Replay));

    [Fact] void should_fail_the_step_saying_the_replay_was_not_started() => _error.Message.ShouldEqual(ObserverOperatorCommandStep.ReplayNotStartedMessage);
    [Fact] void should_not_give_it_again() => _observer.Received(1).Replay();
}
