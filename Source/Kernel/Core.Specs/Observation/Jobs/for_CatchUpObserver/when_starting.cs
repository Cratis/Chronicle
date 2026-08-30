// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver;

/// <summary>
/// Catching up is reached through Subscribe, and behind Subscribe sits the client's registration call with a
/// response timeout on it. Enumerating the observer's event sources and bringing up their steps therefore has to
/// happen after Start has answered, not inside it.
/// </summary>
public class when_starting : given.a_catch_up_observer_job
{
    static readonly Key _key1 = (Key)"partition-1";
    static readonly Key _key2 = (Key)"partition-2";

    void Establish() => _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys(_key1, _key2));

    async Task Because() => await _job.Start(_request);

    [Fact] void should_not_have_prepared_any_steps_yet() => _job.PreparedSteps.ShouldBeNull();
    [Fact] async Task should_not_have_registered_any_partitions_as_catching_up_yet() => await _observer.DidNotReceive().RegisterCatchingUpPartitions(Arg.Any<IEnumerable<Key>>());
    [Fact] void should_leave_the_steps_to_come_up_on_a_later_turn() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
}
