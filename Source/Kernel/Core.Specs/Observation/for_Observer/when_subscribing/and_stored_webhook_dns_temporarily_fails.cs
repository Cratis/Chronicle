// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_stored_webhook_dns_temporarily_fails : given.a_stored_webhook_with_unavailable_dns
{
    bool _subscribedAfterFailure;
    bool _needsRecovery;

    async Task Because()
    {
        await _observer.Subscribe<IWebhookObserverSubscriber>(ObserverType.External, ["Event1"], SiloAddress.Zero);
        _subscribedAfterFailure = await _observer.IsSubscribed();
        _needsRecovery = await _observer.NeedsSubscriptionRecovery(["Event1"]);
    }

    [Fact] void should_not_install_an_unvalidated_subscription() => _subscribedAfterFailure.ShouldBeFalse();
    [Fact] void should_leave_setup_recoverable() => _needsRecovery.ShouldBeTrue();
    [Fact] void should_not_quarantine_on_dns_failure() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Quarantined);
    [Fact] void should_not_create_partition_failures() => _failedPartitionsState.Partitions.ShouldBeEmpty();
}
