// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_restoring_a_client;

public class and_it_was_dropped : given.a_registry_that_notifies_departures
{
    ObserverKey[] _firstNotification;
    ObserverKey[] _secondNotification;

    async Task Establish()
    {
        await Connect(_connection);
        await _clients.RecordObserver(_connection, _first);
        await _clients.OnClientDisconnected(_connection, "stale");
        (_, _firstNotification) = await NextNotification();
        await Restore(_connection);
        await _clients.RecordObserver(_connection, _second);
    }

    async Task Because()
    {
        await _clients.OnClientDisconnected(_connection, "closed");
        (_, _secondNotification) = await NextNotification();
    }

    [Fact] void should_have_notified_the_first_observer_when_it_was_dropped() => _firstNotification.ShouldContainOnly(_first);
    [Fact] void should_only_notify_the_observer_recorded_after_the_restore() => _secondNotification.ShouldContainOnly(_second);
}
