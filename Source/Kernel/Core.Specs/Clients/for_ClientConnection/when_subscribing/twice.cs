// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ClientConnection.when_subscribing;

public class twice : given.a_client_connection
{
    async Task Because()
    {
        await _connection.Connect();
        await _connection.Subscribe(_first);
        await _connection.Subscribe(_first);
        await _connection.Disconnect();
    }

    [Fact] void should_notify_the_subscriber_once() => _firstSubscriber.ReceivedWithAnyArgs(1).ClientDisconnected(default!);
}
