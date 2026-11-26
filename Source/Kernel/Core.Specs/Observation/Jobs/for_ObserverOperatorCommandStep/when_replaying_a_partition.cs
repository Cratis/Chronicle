// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverOperatorCommandStep;

public class when_replaying_a_partition : given.an_observer_to_give_commands_to
{
    async Task Because() => await Give(ObserverOperatorCommand.ReplayPartition, "some-partition");

    [Fact] void should_replay_that_partition() => _observer.Received(1).ReplayPartition((Key)"some-partition");
}
