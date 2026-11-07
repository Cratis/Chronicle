// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_a_webhook_replay_fails : given.a_performing_job_step
{
    static readonly ReplayEpoch _epoch = new(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"));

    void Establish()
    {
        _performState.EventObservationState = EventObservationState.Replay;
        _performState.ReplayEpoch = _epoch;
        _eventCursor.Current.Returns([
            AppendedEvent.EmptyWithEventSequenceNumber(42),
            AppendedEvent.EmptyWithEventSequenceNumber(43)]);
        var subscriber = Substitute.For<IWebhookObserverSubscriber>();
        subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>())
            .Returns(ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "receiver rejected delivery") with { FailurePolicy = WebhookDeliveryFailurePolicy.ParkPartition });
        _jobStep.SetupForTesting(_observer, subscriber, "some-partition");
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_report_the_failure_with_the_replay_epoch() => _observer.Received(1).PartitionFailed("some-partition", 42UL, Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), FailureKind.Handling, null, null, new PartitionFailureContext(WebhookDeliveryFailurePolicy.ParkPartition, null, _epoch));
    [Fact] void should_not_use_the_overload_without_an_epoch() => _observer.DidNotReceive().PartitionFailed(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<FailureKind>(), null, null, Arg.Any<WebhookDeliveryFailurePolicy>());
}
