// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_recording_an_observer;

public class and_it_is_recorded_twice : given.a_registry_that_notifies_departures
{
    ObserverKey[] _notifiedObservers;

    async Task Establish()
    {
        await Connect(_connection);
        await _clients.RecordObserver(_connection, _first);
        await _clients.RecordObserver(_connection, _first);
    }

    async Task Because()
    {
        await _clients.OnClientDisconnected(_connection, "closed");
        (_, _notifiedObservers) = await NextNotification();
    }

    [Fact] void should_notify_the_observer_once() => _notifiedObservers.ShouldContainOnly(_first);
}
