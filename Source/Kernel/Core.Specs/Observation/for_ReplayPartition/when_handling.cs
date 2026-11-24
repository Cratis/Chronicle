// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_ReplayPartition;

public class when_handling : given.a_grain_factory_accepting_operator_commands
{
    async Task Because() => await new ReplayPartition("some-event-store", "some-namespace", "some-observer", string.Empty, "some-partition")
        .Handle(_grainFactory, _storage)
        .WaitAsync(TimeSpan.FromSeconds(5));

    [Fact] void should_accept_the_replay_as_a_job_for_the_partition() => _jobsManager.Received(1).Start<IObserverOperatorCommandJob, ObserverOperatorCommandRequest>(
        new ObserverOperatorCommandRequest(_key, ObserverOperatorCommand.ReplayPartition, "some-partition"));
    [Fact] void should_not_call_the_observer() => _observer.DidNotReceive().ReplayPartition(Arg.Any<Concepts.Keys.Key>());
}
