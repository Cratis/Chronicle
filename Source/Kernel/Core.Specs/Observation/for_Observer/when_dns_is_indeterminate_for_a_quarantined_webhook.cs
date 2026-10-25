// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_dns_is_indeterminate_for_a_quarantined_webhook : given.a_webhook_ready_to_probe
{
    Guid _originalProbe;
    Guid? _retainedProbe;
    bool _reminderRetained;
    bool _subscriptionRetained;

    void Establish()
    {
        _originalProbe = _stateStorage.State.WebhookProbe.Id;
        _webhookTargetPolicy.EvaluateForDelivery(Arg.Any<string>(), Arg.Any<EventStoreName?>(), Arg.Any<CancellationToken>())
            .Returns(new WebhookTargetValidation(false, "DNS temporarily unavailable.") { IsIndeterminate = true });
    }

    async Task Because()
    {
        await _observer.Subscribe<IWebhookObserverSubscriber>(ObserverType.External, ["Event1", "Event2"], SiloAddress.Zero, reactivateRetired: false, automatic: true);
        _retainedProbe = _stateStorage.State.WebhookProbe?.Id;
        _reminderRetained = await _observer.GetReminder(Observer.WebhookProbeReminderName) is not null;
        _subscriptionRetained = await _observer.IsSubscribed();
        await Tick();
    }

    [Fact] void should_preserve_the_existing_probe() => _retainedProbe.ShouldEqual(_originalProbe);
    [Fact] void should_preserve_the_probe_reminder() => _reminderRetained.ShouldBeTrue();
    [Fact] void should_preserve_the_probe_subscription() => _subscriptionRetained.ShouldBeTrue();
    [Fact] void should_still_deliver_the_scheduled_probe() => _webhook.Received(1).OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>());
}
