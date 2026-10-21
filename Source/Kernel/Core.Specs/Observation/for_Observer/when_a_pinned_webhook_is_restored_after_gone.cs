// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_pinned_webhook_is_restored_after_gone : given.a_gone_retired_webhook
{
    async Task Because()
    {
        await Crash();
        _eventTypesStorage.HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>()).Returns(true);
        await _observer.SubscribeWithGenerationDelivery<IWebhookObserverSubscriber>(ObserverType.External, ["Event1", "Event2"], SiloAddress.Zero, EventGenerationDelivery.Pinned, reactivateRetired: false, automatic: true);
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_remain_retired() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Retired);
    [Fact] void should_keep_the_same_incident() => _stateStorage.State.WebhookRetirement.Id.ShouldEqual(_retirementId);
    [Fact] async Task should_not_subscribe() => (await _observer.IsSubscribed()).ShouldBeFalse();
}
