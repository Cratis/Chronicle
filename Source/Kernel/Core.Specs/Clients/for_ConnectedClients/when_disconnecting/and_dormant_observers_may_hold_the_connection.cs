// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_disconnecting;

public class and_dormant_observers_may_hold_the_connection : given.a_registry_that_notifies_departures
{
    readonly TaskCompletionSource _told = new();

    async Task Establish()
    {
        _notifier.NotifyDormant(Arg.Any<ConnectionId>(), Arg.Any<IReadOnlyCollection<Concepts.Observation.ObserverKey>>()).Returns(_ =>
        {
            _told.TrySetResult();
            return Task.CompletedTask;
        });
        await Connect(_connection);
    }

    async Task Because()
    {
        await _clients.OnClientDisconnected(_connection, "closed");

        // The notification runs in the background, outside the registry, so it is waited for.
        await _told.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_have_the_dormant_observers_that_recorded_it_told_too() => _notifier.Received(1).NotifyDormant(_connection, Arg.Any<IReadOnlyCollection<Concepts.Observation.ObserverKey>>());
}
