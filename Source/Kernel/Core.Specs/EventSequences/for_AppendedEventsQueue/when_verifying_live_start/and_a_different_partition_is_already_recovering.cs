// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_verifying_live_start;

public class and_a_different_partition_is_already_recovering : given.a_queue_verifying_live_gaps
{
    void Establish()
    {
        _history = [MatchingAt(102, "Q")];
        _observer.GetState().Returns(new ObserverState
        {
            NextEventSequenceNumber = 102UL,
            RunningState = ObserverRunningState.Active,
            CatchingUpPartitions = new HashSet<Key> { "P" }
        });
    }

    Task Because() => Deliver(MatchingAt(105, "R"));

    [Fact] void should_not_assume_the_other_partition_owns_the_missing_event() => _handledEvents.ShouldEqual(0);
    [Fact] void should_conservatively_request_whole_observer_catch_up() => _observer.Received(1).CatchUp();
}
