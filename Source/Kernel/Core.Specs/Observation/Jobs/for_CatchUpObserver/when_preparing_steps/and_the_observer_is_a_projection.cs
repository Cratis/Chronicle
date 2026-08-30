// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_preparing_steps;

/// <summary>
/// A projection catches up through one step that walks the sequence in global order. A step per event source
/// would buy it nothing - a collapsing projection funnels every event source through one subscriber activation
/// anyway - while costing a grain activation and two storage writes each, inside the call that registered it.
/// </summary>
public class and_the_observer_is_a_projection : given.a_catch_up_observer_job
{
    static readonly Key _key1 = (Key)"partition-1";
    static readonly Key _key2 = (Key)"partition-2";

    void Establish() => _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys(_key1, _key2));

    async Task Because() => await StartAndLetTheStepsCome();

    [Fact] void should_prepare_a_single_step() => _job.PreparedSteps!.Count.ShouldEqual(1);
    [Fact] void should_prepare_a_step_that_handles_events_for_the_whole_observer() => _job.PreparedSteps![0].Type.ShouldEqual(typeof(IHandleEventsForObserver));
    [Fact] void should_tell_the_step_to_skip_failed_partitions() => ((HandleEventsForObserverArguments)_job.PreparedSteps![0].Request).SkipFailedPartitions.ShouldBeTrue();
}
