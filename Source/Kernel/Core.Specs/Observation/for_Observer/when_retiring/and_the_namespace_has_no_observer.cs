// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer.when_retiring;

public class and_the_namespace_has_no_observer : given.an_absent_observer
{
    async Task Establish() => await Crash();

    async Task Because() => await _observer.Retire();

    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_allocate_a_lifecycle() => _stateStorage.State.AlertLifecycleId.ShouldEqual(Guid.Empty);
    [Fact] async Task should_not_create_a_reminder() => (await _observer.GetReminder(Observer.AlertReminderName)).ShouldBeNull();
    [Fact] async Task should_not_activate_the_tracker() => await _observerAlerts.DidNotReceive().Reconcile(Arg.Any<ObserverAlertSnapshot>());
}
