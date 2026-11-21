// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// A queued event below the cursor is not covered by another partition's recovery. It must become durable catch-up work.
/// </summary>
public class and_an_unowned_event_was_passed_by_partition_recovery : given.an_observer_with_skip_detection
{
    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 102UL };
        await _observer.PartitionFailed("P", 100UL, ["Failed handler"], string.Empty);
        await _observer.FailedPartitionRecovered("P", 103UL);
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 102UL)]);
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 102UL)]);
    }

    [Fact]
    void should_start_one_catch_up_from_the_missed_event() => _jobsManager.Received(1)
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(request => request.Key == (Key)"Q" && request.FromSequenceNumber == (EventSequenceNumber)102UL));
    [Fact] void should_keep_the_missed_delivery_pending() => _stateStorage.State.CatchingUpPartitions.ShouldContain((Key)"Q");
    [Fact] void should_report_the_suspected_skip() => _suspectedSkips.ShouldEqual(1);
}
