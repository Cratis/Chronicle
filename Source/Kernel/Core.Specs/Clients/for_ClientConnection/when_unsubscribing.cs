// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ClientConnection;

public class when_unsubscribing : given.a_client_connection
{
    async Task Establish()
    {
        await _connection.Connect();
        await _connection.Subscribe(_first);
    }

    async Task Because()
    {
        await _connection.Unsubscribe(_first);
        await _connection.Disconnect();
    }

    [Fact] void should_not_notify_the_subscriber() => _firstSubscriber.DidNotReceiveWithAnyArgs().ClientDisconnected(default!);
}
