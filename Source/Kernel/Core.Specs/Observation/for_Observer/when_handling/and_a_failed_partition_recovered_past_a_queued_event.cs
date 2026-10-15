// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// Reproduces https://github.com/Cratis/Chronicle/issues/4748: P recovers at 103 before queued Q@102 arrives.
/// Correct behavior must deliver Q@102 despite the unrelated partition's recovery position.
/// </summary>
public class and_a_failed_partition_recovered_past_a_queued_event : given.an_observer_with_skip_detection
{
    readonly TaskCompletionSource _deliveryQueued = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseDelivery = new(TaskCreationOptions.RunContinuationsAsynchronously);

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 102UL };
        await _observer.PartitionFailed("P", 100UL, ["Failed handler"], string.Empty);
        _subscriber.ClearReceivedCalls();
    }

    async Task Because()
    {
        // The queue is held behind another observer; recovery reads storage independently of its delivery.
        var delivery = DeliverQueuedEvent();
        await _deliveryQueued.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        try
        {
            await _observer.FailedPartitionRecovered("P", 103UL);
        }
        finally
        {
            _releaseDelivery.TrySetResult();
            await delivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
    }

    async Task DeliverQueuedEvent()
    {
        _deliveryQueued.TrySetResult();
        await _releaseDelivery.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 102UL)]);
    }

    [Fact] void should_currently_skip_the_queued_event() => _subscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_currently_advance_past_the_queued_event() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)104UL);
    [Fact] void should_report_one_suspected_skip() => _suspectedSkips.ShouldEqual(1);
    [Fact] void should_clear_the_recovered_failure() => _failedPartitionsStorage.State.IsFailed((Key)"P").ShouldBeFalse();
}
