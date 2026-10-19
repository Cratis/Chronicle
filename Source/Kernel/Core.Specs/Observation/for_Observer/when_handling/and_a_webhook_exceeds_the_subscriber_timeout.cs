// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_a_webhook_exceeds_the_subscriber_timeout : given.an_observer_with_subscription_for_specific_event_type
{
    bool _cancelled;

    protected override Observers CreateObserversConfig() => new() { SubscriberTimeout = 1, WriteCoalescingInterval = 0 };

    void Establish()
    {
        var webhook = Substitute.For<IWebhookObserverSubscriber>();
        _subscriber = webhook;
        webhook.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, TimeProvider.System, call.Arg<CancellationToken>());
                    return ObserverSubscriberResult.Ok(EventSequenceNumber.First);
                }
                finally
                {
                    _cancelled = true;
                }
            });
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 42UL, LastHandledEventSequenceNumber = 41UL };
    }

    async Task Because() => await _observer.Handle("partition", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);

    [Fact] void should_cancel_and_drain_live_delivery() => _cancelled.ShouldBeTrue();
    [Fact] void should_not_advance_the_partition() => _stateStorage.State.LastHandledEventSequenceNumber.Value.ShouldEqual(41UL);
    [Fact] void should_record_only_one_timeout() => _failedPartitionsState.Partitions.Single().Attempts.Count().ShouldEqual(1);
    [Fact] void should_classify_the_timeout() => _failedPartitionsState.Partitions.Single().LastAttempt.Kind.ShouldEqual(FailureKind.Timeout);
}
