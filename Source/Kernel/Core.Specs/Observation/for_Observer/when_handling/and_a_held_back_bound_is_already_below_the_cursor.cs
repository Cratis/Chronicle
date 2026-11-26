// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_a_held_back_bound_is_already_below_the_cursor : given.an_observer_with_skip_detection
{
    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 100UL };
        await _observer.RegisterCatchingUpPartitions(new HashSet<Key> { "P" });
        await _observer.Handle("P", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 50UL)]);
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(105UL));
    }

    async Task Because() => await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 105UL)]);

    [Fact] void should_advance_past_the_older_bound() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)106UL);
    [Fact] void should_leave_the_held_partition_owned_by_its_job() => _stateStorage.State.CatchingUpPartitions.ShouldContain((Key)"P");
    [Fact] void should_not_start_a_competing_repair() => CheckDidNotStartCatchupJob();
}
