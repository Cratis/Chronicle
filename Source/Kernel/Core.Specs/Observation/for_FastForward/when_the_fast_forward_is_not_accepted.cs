// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.Observation.for_FastForward;

public class when_the_fast_forward_is_not_accepted : given.a_grain_factory_accepting_operator_commands
{
    Exception _error;

    void Establish() => _jobsManager.Start<Jobs.IObserverOperatorCommandJob, Jobs.ObserverOperatorCommandRequest>(Arg.Any<Jobs.ObserverOperatorCommandRequest>())
        .Returns(Task.FromResult(Cratis.Monads.Result<JobId, StartJobError>.Failed(StartJobError.NotAdmitted)));

    async Task Because() => _error = await Catch.Exception(() => new FastForward("some-event-store", "some-namespace", "some-observer", string.Empty).Handle(_grainFactory, _storage));

    [Fact] void should_fail_the_command() => _error.ShouldBeOfExactType<ObserverOperatorCommandNotAccepted>();
}
