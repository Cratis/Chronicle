// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Clients.for_ClientConnection.when_disconnecting;

public class and_a_subscriber_fails : given.a_client_connection
{
    Exception _exception;

    async Task Establish()
    {
        _firstSubscriber.ClientDisconnected(Arg.Any<ClientConnectionEpoch>()).Returns(_ => throw new InvalidOperationException("boom"));
        await _connection.Connect();
        await _connection.Subscribe(_first);
        await _connection.Subscribe(_second);
    }

    async Task Because() => _exception = await Catch.Exception(_connection.Disconnect);

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_still_notify_the_other_subscriber() => _secondSubscriber.ReceivedWithAnyArgs(1).ClientDisconnected(default!);
}
