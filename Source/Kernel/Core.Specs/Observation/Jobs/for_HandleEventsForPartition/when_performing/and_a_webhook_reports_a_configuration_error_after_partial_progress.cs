// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_a_webhook_reports_a_configuration_error_after_partial_progress : given.a_performing_job_step
{
    void Establish()
    {
        _eventCursor.Current.Returns([
            AppendedEvent.EmptyWithEventSequenceNumber(42),
            AppendedEvent.EmptyWithEventSequenceNumber(43)]);
        var subscriber = Substitute.For<IWebhookObserverSubscriber>();
        subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>())
            .Returns(ObserverSubscriberResult.Failed(new(42), "receiver rejected delivery") with { FailurePolicy = WebhookDeliveryFailurePolicy.PauseObserver });
        _jobStep.SetupForTesting(_observer, subscriber, "some-partition");
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_forward_the_receiver_policy_at_the_first_unhandled_event() => _observer.Received(1).PartitionFailed("some-partition", 43UL, Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), FailureKind.Handling, null, null, new(WebhookDeliveryFailurePolicy.PauseObserver));
}
