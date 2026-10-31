// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ClientConnection;

public class when_connected : given.a_client_connection
{
    async Task Because()
    {
        await _connection.Connect();
        await _connection.Subscribe(_first);
        await _connection.Unsubscribe(_first);
    }

    [Fact] void should_be_kept_alive() => typeof(ClientConnection).GetCustomAttributes(typeof(KeepAliveAttribute), true).ShouldNotBeEmpty();
    [Fact] void should_not_request_deactivation() => _connection.ReleaseCount.ShouldEqual(0);
}
