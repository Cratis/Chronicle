// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_activated;

public class and_removal_cleanup_is_incomplete : given.an_observer
{
    async Task Establish()
    {
        _eventStoreNamespaceStorage.FailedPartitions.RemoveAllFor(_observerId).Returns(Task.FromException(new Exception("Cleanup failed")));
        await Catch.Exception(_observer.Remove);
    }

    async Task Because()
    {
        await Crash();
        await _observer.Subscribe<ObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero, reactivateRetired: false);
        await _observer.SubscribeToAllEvents<ObserverSubscriber>(ObserverType.Reactor, SiloAddress.Zero, reactivateRetired: false);
    }

    [Fact] void should_remain_retired() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Retired);
    [Fact] async Task should_not_restore_the_subscription() => (await _observer.IsSubscribed()).ShouldBeFalse();
    [Fact] async Task should_retain_the_alert_reminder() => (await _observer.GetReminder(Observer.AlertReminderName)).ShouldNotBeNull();
}
