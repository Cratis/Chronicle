// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Webhooks.for_Webhooks;

public class when_dns_recovers_after_activation : given.a_webhook_manager_with_unavailable_dns
{
    bool _initiallySubscribed;
    bool _eventuallySubscribed;

    async Task Because()
    {
        _manager = await _managerSilo.CreateGrainAsync<Webhooks>("orders");
        _initiallySubscribed = await _observer.IsSubscribed();
        await _manager.ReceiveReminder(_reminderName, Tick(TimeSpan.FromMinutes(1)));
        await _manager.ReceiveReminder(_reminderName, Tick(TimeSpan.FromMinutes(30)));
        _dnsAvailable = true;
        await _manager.ReceiveReminder(_reminderName, Tick(TimeSpan.FromMinutes(2)));
        _eventuallySubscribed = await _observer.IsSubscriptionEstablished();
    }

    [Fact] void should_activate_without_installing_an_unvalidated_subscription() => _initiallySubscribed.ShouldBeFalse();
    [Fact] void should_schedule_an_automatic_retry() => ShouldSchedule(TimeSpan.FromMinutes(1));
    [Fact] void should_back_off_when_dns_is_still_down() => ShouldSchedule(TimeSpan.FromMinutes(2));
    [Fact] void should_cap_the_backoff() => ShouldSchedule(TimeSpan.FromMinutes(10));
    [Fact] void should_subscribe_when_the_scheduled_retry_observes_dns_recovery() => _eventuallySubscribed.ShouldBeTrue();
    [Fact] async Task should_remove_the_retry_reminder_after_recovery() => (await _manager.GetReminder(_reminderName)).ShouldBeNull();
    [Fact] void should_never_quarantine_the_observer() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Quarantined);
}
