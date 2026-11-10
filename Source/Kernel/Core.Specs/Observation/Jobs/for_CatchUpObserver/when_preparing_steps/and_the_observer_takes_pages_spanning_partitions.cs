// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_preparing_steps;

/// <summary>
/// An observer that declared it takes pages spanning partitions is caught up in one walk, so the pages it receives are full.
/// </summary>
public class and_the_observer_takes_pages_spanning_partitions : given.a_catch_up_observer_job
{
    static readonly Key _key1 = (Key)"partition-1";
    static readonly Key _key2 = (Key)"partition-2";

    void Establish()
    {
        _request = _request with { ObserverType = ObserverType.Reactor };
        _observer.CatchesUpInOneSweep().Returns(true);
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys(_key1, _key2));
    }

    async Task Because() => await StartAndLetTheStepsCome();

    [Fact] void should_prepare_a_single_step() => _job.PreparedSteps!.Count.ShouldEqual(1);
    [Fact] void should_prepare_a_step_that_handles_events_for_the_whole_observer() => _job.PreparedSteps!.Single().Type.ShouldEqual(typeof(IHandleEventsForObserver));
    [Fact] void should_still_register_every_partition_as_catching_up() => _observer.Received(1).RegisterCatchingUpPartitions(Arg.Is<IEnumerable<Key>>(keys => keys.Count() == 2));
}
