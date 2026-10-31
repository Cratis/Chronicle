// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ClientConnection.when_disconnecting;

public class and_subscribers_were_notified : given.a_client_connection
{
    async Task Establish()
    {
        await _connection.Connect();
        await _connection.Subscribe(_first);
    }

    Task Because() => _connection.Disconnect();

    [Fact] void should_notify_the_subscriber() => _firstSubscriber.ReceivedWithAnyArgs(1).ClientDisconnected(default!);
    [Fact] void should_request_deactivation_once() => _connection.ReleaseCount.ShouldEqual(1);
}
