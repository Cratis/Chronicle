// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Clients.for_ClientConnection.when_subscribing;

public class after_disconnect : given.a_client_connection
{
    ClientConnectionEpoch _epoch;

    async Task Establish()
    {
        await _connection.Connect();
        await _connection.Disconnect();
    }

    async Task Because()
    {
        _epoch = await _connection.Subscribe(_first);
        await _connection.Disconnect();
    }

    [Fact] void should_return_not_set() => _epoch.ShouldEqual(ClientConnectionEpoch.NotSet);
    [Fact] void should_never_notify_the_subscriber() => _firstSubscriber.DidNotReceiveWithAnyArgs().ClientDisconnected(default!);
}
