// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_an_ordinary_failure_recovers_after_reactivation : given.an_observer_with_subscription_for_specific_event_type
{
    EventSequenceNumber _cursorBeforeReactivation;

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 50UL };
        await _observer.PartitionFailed("P", 50UL, ["Failed handler"], string.Empty);
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 100UL };
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(105UL));
    }

    async Task Because()
    {
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 105UL)]);
        _cursorBeforeReactivation = _stateStorage.State.NextEventSequenceNumber;
        await Crash();
        _observer.SetSubscription(subscription);
        _jobsManager.ClearReceivedCalls();
        await _observer.TryStartRecoverJobForFailedPartition("P");
        await _observer.FailedPartitionRecovered("P", 50UL);
    }

    [Fact] void should_advance_past_the_older_failure() => _cursorBeforeReactivation.ShouldEqual((EventSequenceNumber)106UL);
    [Fact] void should_retry_from_the_failed_partitions_own_position() => _jobsManager.Received(1)
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(_ => _.Key == (Key)"P" && _.FromSequenceNumber == (EventSequenceNumber)50UL));
    [Fact] void should_clear_the_recovered_failure() => _failedPartitionsStorage.State.Partitions.ShouldBeEmpty();
    [Fact] void should_not_rewind_the_shared_cursor_for_recovery() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)106UL);
}
