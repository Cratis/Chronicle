// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_disconnecting;

public class and_the_notification_is_slow_and_fails : given.a_registry_that_notifies_departures
{
    readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _disconnectCompleted;
    bool _otherStillConnected;

    async Task Establish()
    {
        _notifier
            .Notify(Arg.Any<ConnectionId>(), Arg.Any<IReadOnlyCollection<ObserverKey>>())
            .Returns(async _ =>
            {
                _started.SetResult();
                await _release.Task;
                throw new IOException("Simulated notification failure");
            });
        await Connect(_connection);
        await Connect(_otherConnection);
        await _clients.RecordObserver(_connection, _first);
    }

    async Task Because()
    {
        var disconnect = _clients.OnClientDisconnected(_connection, "closed");
        await _started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _disconnectCompleted = disconnect.IsCompletedSuccessfully;
        _otherStillConnected = await _clients.IsConnected(_otherConnection);
        _release.SetResult();
        await disconnect.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_finish_disconnect_without_waiting_for_the_notification() => _disconnectCompleted.ShouldBeTrue();
    [Fact] void should_keep_answering_for_other_clients() => _otherStillConnected.ShouldBeTrue();
}
