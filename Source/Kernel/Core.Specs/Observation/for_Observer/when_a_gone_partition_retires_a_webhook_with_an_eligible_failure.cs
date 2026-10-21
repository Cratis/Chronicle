// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Webhooks;
using Orleans.TestKit;

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_gone_partition_retires_a_webhook_with_an_eligible_failure : given.a_webhook_with_a_definition
{
    IWebhookObserverSubscriber _webhook;

    async Task Establish()
    {
        _webhook = Substitute.For<IWebhookObserverSubscriber>();
        _silo.AddProbe<IWebhookObserverSubscriber>(_ => _webhook);
        await _observer.PartitionFailed("recoverable", 41UL, ["temporary outage"], string.Empty, FailureKind.Handling);
    }

    async Task Because()
    {
        await _observer.PartitionFailed("gone", 42UL, ["Gone"], string.Empty, FailureKind.Handling, failurePolicy: WebhookDeliveryFailurePolicy.RetireWebhook, deliveryAttempt: new WebhookDeliveryAttempt(_stateStorage.State.AlertLifecycleId, WebhookDeliveryAttempt.Fingerprint(_webhookDefinition)));
        await _observer.RunWatchdogAsync();
        _webhook.ClearReceivedCalls();
        await _silo.FireAllTimersAsync();
    }

    [Fact] void should_retire_the_source() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Retired);
    [Fact] void should_not_start_a_probe() => _stateStorage.State.WebhookProbe.ShouldBeNull();
    [Fact] void should_not_contact_the_receiver() => _webhook.DidNotReceive().OnNext(
        Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>());
    [Fact] void should_preserve_both_failed_partitions() => _failedPartitionsState.Partitions.Count().ShouldEqual(2);
}
