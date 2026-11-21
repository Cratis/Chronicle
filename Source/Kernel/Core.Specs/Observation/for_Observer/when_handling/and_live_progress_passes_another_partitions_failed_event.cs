// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// Live progress on Q leaves P's independent recovery position intact without pinning the shared cursor.
/// </summary>
public class and_live_progress_passes_another_partitions_failed_event : given.an_observer_with_skip_detection
{
    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 102UL };
        await _observer.PartitionFailed("P", 102UL, ["Failed handler"], string.Empty);
        _subscriber.OnNext((Key)"Q", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(105UL));
    }

    async Task Because() => await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 105UL)]);

    [Fact] void should_advance_the_shared_cursor() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)106UL);
    [Fact] void should_retain_the_failed_partitions_recovery_position() => _failedPartitionsStorage.State.Partitions.Single().LastAttempt.SequenceNumber.ShouldEqual((EventSequenceNumber)102UL);
    [Fact] void should_keep_the_failure_pending() => _failedPartitionsStorage.State.IsFailed((Key)"P").ShouldBeTrue();
    [Fact] void should_record_the_successful_live_delivery() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)105UL);
}
