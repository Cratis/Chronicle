// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Storage.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_gone_partition_retires_a_webhook_with_an_eligible_failure : given.a_webhook_with_a_definition
{
    async Task Establish()
    {
        await _observer.PartitionFailed("recoverable", 41UL, ["temporary outage"], string.Empty, FailureKind.Handling);
    }

    async Task Because()
    {
        await _observer.PartitionFailed("gone", 42UL, ["Gone"], string.Empty, FailureKind.Handling,
            WebhookDeliveryFailurePolicy.RetireWebhook,
            new WebhookDeliveryAttempt(_stateStorage.State.AlertLifecycleId, WebhookDeliveryAttempt.Fingerprint(_webhookDefinition)));
        await _observer.RunWatchdogAsync();
        _webhook.ClearReceivedCalls();
        await _silo.Timers.FireAllAsync();
    }

    [Fact] void should_retire_the_source() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Retired);
    [Fact] void should_not_start_a_probe() => _stateStorage.State.WebhookProbe.ShouldBeNull();
    [Fact] void should_not_contact_the_receiver() => _webhook.DidNotReceive().OnNext(
        Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>());
    [Fact] void should_preserve_both_failed_partitions() => _failedPartitionsState.Partitions.Count().ShouldEqual(2);
}
