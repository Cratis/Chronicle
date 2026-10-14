// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_caught_up;

/// <summary>
/// The completion records what the partition's job covered before anything is evaluated, so an event held back for it
/// that another partition has already moved the position past is not suspected.
/// </summary>
public class and_the_cursor_had_already_passed_the_held_back_event : for_Observer.given.an_observer_with_skip_detection
{
    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 9UL };
        await _observer.RegisterCatchingUpPartitions(["Q"]);
        _subscriber.OnNext((Key)"B", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(12UL));
        EventSequenceDoesNotHaveNextEvent(10UL);
    }

    async Task Because()
    {
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 10UL)]);
        await _observer.Handle("B", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 12UL)]);
        await _observer.PartitionCaughtUp("Q", 10UL, 10UL);
    }

    [Fact] void should_have_moved_the_position_past_the_held_back_event() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)13UL);
    [Fact] void should_not_measure_a_suspected_skip() => _metrics.For(EventsSuspectedSkipped).ShouldBeEmpty();
    [Fact] void should_not_warn() => _recordedLogs.SuspectedSkipWarnings.ShouldBeEmpty();
}
