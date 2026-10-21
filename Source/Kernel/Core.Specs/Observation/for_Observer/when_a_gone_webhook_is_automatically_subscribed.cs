// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_gone_webhook_is_automatically_subscribed : given.a_gone_retired_webhook
{
    async Task Because()
    {
        await Crash();
        await _observer.Subscribe<IWebhookObserverSubscriber>(ObserverType.External, ["Event1", "Event2"], SiloAddress.Zero, reactivateRetired: false, automatic: true);
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_remain_retired() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Retired);
    [Fact] void should_keep_the_incident_identity() => _stateStorage.State.WebhookRetirement.Id.ShouldEqual(_retirementId);
    [Fact] void should_keep_only_the_authority() => _stateStorage.State.WebhookRetirement.Authority.ShouldEqual("https://receiver.example:8443");
    [Fact] async Task should_not_subscribe() => (await _observer.IsSubscribed()).ShouldBeFalse();
}
