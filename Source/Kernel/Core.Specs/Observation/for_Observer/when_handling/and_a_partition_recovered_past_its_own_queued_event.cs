// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// The partition's own recovery moved the position past its queued event: its own job covered it, so nothing is suspected.
/// </summary>
public class and_a_partition_recovered_past_its_own_queued_event : given.an_observer_with_skip_detection
{
    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 102UL };
        await _observer.PartitionFailed("P", 100UL, ["Failed handler"], string.Empty);
        _subscriber.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.FailedPartitionRecovered("P", 103UL);
        await _observer.Handle("P", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 102UL)]);
    }

    [Fact] void should_have_advanced_past_the_queued_event() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)104UL);
    [Fact] void should_not_measure_a_suspected_skip() => _metrics.For(EventsSuspectedSkipped).ShouldBeEmpty();
    [Fact] void should_not_warn() => _recordedLogs.SuspectedSkipWarnings.ShouldBeEmpty();
}
