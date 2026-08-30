// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_preparing_steps;

/// <summary>
/// Only a projection collapses onto one subscriber activation. Every other observer type keeps a step per event
/// source, where the partitions really do get handled independently.
/// </summary>
public class and_the_observer_is_a_reactor : given.a_catch_up_observer_job
{
    static readonly Key _key1 = (Key)"partition-1";
    static readonly Key _key2 = (Key)"partition-2";

    void Establish()
    {
        _request = _request with { ObserverType = ObserverType.Reactor };
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys(_key1, _key2));
    }

    async Task Because() => await StartAndLetTheStepsCome();

    [Fact] void should_prepare_a_step_per_partition() => _job.PreparedSteps!.Count.ShouldEqual(2);
    [Fact] void should_prepare_steps_that_handle_events_for_a_partition() => _job.PreparedSteps!.All(step => step.Type == typeof(IHandleEventsForPartition)).ShouldBeTrue();
}
