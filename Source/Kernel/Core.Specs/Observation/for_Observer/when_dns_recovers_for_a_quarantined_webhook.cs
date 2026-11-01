// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_dns_recovers_for_a_quarantined_webhook : given.a_webhook_ready_to_probe
{
    bool _dnsAvailable;
    bool _quarantineAndProbeRetained;
    bool _quarantinedAfterRecovery;
    bool _probeReminderRemoved;

    void Establish()
    {
        _webhookTargetPolicy.EvaluateForDelivery(Arg.Any<string>(), Arg.Any<EventStoreName?>(), Arg.Any<CancellationToken>())
            .Returns(_ => _dnsAvailable
                ? new WebhookTargetValidation(true, string.Empty)
                : new WebhookTargetValidation(false, "DNS temporarily unavailable.") { IsIndeterminate = true });
        _webhook.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>())
            .Returns(_ => _dnsAvailable
                ? ObserverSubscriberResult.Ok(42UL)
                : ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "DNS temporarily unavailable."));
    }

    async Task Because()
    {
        var originalProbe = _stateStorage.State.WebhookProbe.Id;
        await _observer.Subscribe<IWebhookObserverSubscriber>(
            ObserverType.External,
            ["Event1", "Event2"],
            SiloAddress.Zero,
            reactivateRetired: false,
            automatic: true);
        await Tick();
        _quarantineAndProbeRetained = await _observer.IsObserverQuarantined() &&
            _stateStorage.State.WebhookProbe?.Id == originalProbe &&
            await _observer.GetReminder(Observer.WebhookProbeReminderName) is not null;

        _dnsAvailable = true;
        _now = _stateStorage.State.WebhookProbe.Due;
        await Tick();
        _quarantinedAfterRecovery = await _observer.IsObserverQuarantined();
        _probeReminderRemoved = await _observer.GetReminder(Observer.WebhookProbeReminderName) is null;
    }

    [Fact] void should_preserve_quarantine_and_the_registered_probe_during_the_dns_outage() => _quarantineAndProbeRetained.ShouldBeTrue();
    [Fact] void should_leave_quarantine() => _quarantinedAfterRecovery.ShouldBeFalse();
    [Fact] void should_unregister_the_probe_reminder() => _probeReminderRemoved.ShouldBeTrue();
    [Fact] void should_remove_probe_control() => _stateStorage.State.WebhookProbe.ShouldBeNull();
    [Fact] void should_attempt_delivery_again_after_dns_returns() => _webhook.Received(2).OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>());
}
