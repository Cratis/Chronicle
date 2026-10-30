// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Webhooks.for_Webhooks;

public class when_dns_permanently_fails_during_a_retry : given.a_webhook_manager_with_unavailable_dns
{
    async Task Because()
    {
        _manager = await _managerSilo.CreateGrainAsync<Webhooks>("orders");
        _dnsError = SocketError.HostNotFound;
        await _manager.ReceiveReminder(_reminderName, Tick(TimeSpan.FromMinutes(1)));
    }

    [Fact] void should_quarantine_after_the_permanent_rejection() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] async Task should_remove_the_subscription_retry_reminder() => (await _manager.GetReminder(_reminderName)).ShouldBeNull();
    [Fact] void should_not_establish_live_delivery() => _appendedEventsQueues.DidNotReceive().Subscribe(_observerKey, Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
}
