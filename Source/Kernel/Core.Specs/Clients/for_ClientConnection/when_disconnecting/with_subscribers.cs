// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Clients.for_ClientConnection.when_disconnecting;

public class with_subscribers : given.a_client_connection
{
    ClientConnectionEpoch _epoch;

    async Task Establish()
    {
        await _connection.Connect();
        _epoch = await _connection.Subscribe(_first);
        await _connection.Subscribe(_second);
    }

    Task Because() => _connection.Disconnect();

    [Fact] void should_notify_the_first_subscriber_once() => _firstSubscriber.Received(1).ClientDisconnected(_epoch);
    [Fact] void should_notify_the_second_subscriber_once() => _secondSubscriber.Received(1).ClientDisconnected(_epoch);
}
