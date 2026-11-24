// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Reactors.Clients;

namespace Cratis.Chronicle.Observation.for_Observer.when_going_dormant;

/// <summary>
/// An observer with an active client and two standbys is dormant once the period has passed, and records both standbys oldest first.
/// </summary>
public class and_there_are_two_standbys : given.a_client_observer_with_a_standby
{
    static readonly EventType _newestEventType = new("0d6c3f0c-3c70-4d3a-9c12-2f2f9f0c9d11", EventTypeGeneration.First);
    ConnectedClient _newestClient;

    async Task Establish()
    {
        _newestClient = new ConnectedClient { ConnectionId = "c-connection", Version = "3.0.0" };
        await _observer.Subscribe<IReactorObserverSubscriber>(ObserverType.Reactor, [_newestEventType], SiloAddress.Zero, _newestClient);
        await _silo.TimerRegistry.FireAllAsync();
        await _observer.ReceiveReminder(Observer.AlertReminderName, default);
    }

    async Task Because()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_dormantPeriod);
    }

    [Fact] void should_release_the_activation() => ReleaseRequests.ShouldEqual(1);
    [Fact] void should_record_the_newest_client_as_the_target() => Recorded!.Targets.Select(target => target.ConnectionId).ShouldContainOnly(_newestClient.ConnectionId.Value);
    [Fact] void should_record_both_standbys_oldest_first() => Recorded!.Standbys!.Select(standby => standby.ConnectionId).ShouldEqual([_client.ConnectionId.Value, _activeClient.ConnectionId.Value]);
}
