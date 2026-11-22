// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverOperatorCommandStep;

/// <summary>
/// A call to a busy observer times out although the command is fine and still queued; the step gives it again.
/// </summary>
public class when_giving_a_replay_to_an_observer_that_is_busy : given.an_observer_to_give_commands_to
{
    void Establish() => _observer.Replay().Returns(
        Task.FromException<JobId>(new TimeoutException()),
        Task.FromException<JobId>(new TimeoutException()),
        Task.FromResult(JobId.New()));

    async Task Because() => await Give(ObserverOperatorCommand.Replay);

    [Fact] void should_give_the_replay_until_the_observer_answers() => _observer.Received(3).Replay();
}
