// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_caught_up;

/// <summary>
/// The probe after the completion finds the events held back beyond the partition's reach and starts a job for them,
/// so they are covered even though the position has moved past them meanwhile.
/// </summary>
public class and_held_back_events_lie_beyond_its_reach : for_Observer.given.an_observer_with_skip_detection
{
    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 9UL };
        await _observer.RegisterCatchingUpPartitions(["Q"]);
        _subscriber.OnNext((Key)"B", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(25UL));
        EventSequenceHasNextEvent(5UL);
    }

    async Task Because()
    {
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 10UL)]);
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 20UL)]);
        await _observer.Handle("B", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 25UL)]);
        await _observer.PartitionCaughtUp("Q", 5UL, 5UL);
    }

    [Fact] void should_start_a_job_for_the_events_beyond_its_reach() => CheckStartedCatchupJob(5UL, "Q");
    [Fact] void should_not_measure_a_suspected_skip() => _metrics.For(EventsSuspectedSkipped).ShouldBeEmpty();
    [Fact] void should_not_warn() => _recordedLogs.SuspectedSkipWarnings.ShouldBeEmpty();
}
