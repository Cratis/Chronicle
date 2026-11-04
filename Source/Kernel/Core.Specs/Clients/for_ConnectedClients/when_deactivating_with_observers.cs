// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClients;

public class when_deactivating_with_observers : given.a_registry_that_notifies_departures
{
    async Task Establish()
    {
        await Connect(_connection);
        await _clients.RecordObserver(_connection, _first);
    }

    async Task Because() => await _clients.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.None, string.Empty), CancellationToken.None);

    [Fact] void should_not_notify_anything() => _notifier.DidNotReceiveWithAnyArgs().Notify(_connection, []);
}
