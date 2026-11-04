// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_ownership_moves;

public class and_a_webhook_probe_reminder_exists : given.a_live_observer_with_guarded_state
{
    IGrainReminder _reminder;
    ObserverState _before;

    async Task Establish()
    {
        _reminder = await _observer.RegisterOrUpdateReminder(Observer.WebhookProbeReminderName, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because()
    {
        await MoveDefinition();
        _before = await _guarded.Get(_observerId);
        await _observer.RunWatchdogAsync();
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_unregister_the_probe_reminder_once() => _silo.ReminderRegistry.Mock.Verify(registry => registry.UnregisterReminder(It.IsAny<GrainId>(), _reminder), Times.Once);
    [Fact] async Task should_leave_the_probe_reminder_unregistered() => (await _observer.GetReminder(Observer.WebhookProbeReminderName)).ShouldBeNull();
    [Fact] async Task should_not_write_the_new_owners_state() => (await _guarded.Get(_observerId)).ShouldEqual(_before);
    [Fact] void should_not_start_or_resume_jobs() => _jobsManager.ReceivedCalls().ShouldBeEmpty();
}
