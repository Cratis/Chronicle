// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_concluding_partition_catch_up;

/// <summary>
/// Routing has already released the partition, so live delivery owns it and the step must not read on over the events
/// it delivers.
/// </summary>
public class and_the_partition_is_no_longer_catching_up : given.an_observer_with_subscription_for_specific_event_type
{
    bool _concluded;

    void Establish() => _eventSequence.ClearReceivedCalls();

    async Task Because() => _concluded = await _observer.ConcludePartitionCatchUp("partition", 10UL, [event_type]);

    [Fact] void should_conclude_the_partition() => _concluded.ShouldBeTrue();
    [Fact] void should_not_look_for_unread_events() => _eventSequence.DidNotReceive().GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>());
}
