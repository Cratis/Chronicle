// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ClientConnection;

public class when_unsubscribing_after_disconnect : given.a_client_connection
{
    async Task Establish()
    {
        await _connection.Connect();
        await _connection.Disconnect();
    }

    Task Because() => _connection.Unsubscribe(_first);

    [Fact] void should_request_deactivation_again() => _connection.ReleaseCount.ShouldEqual(2);
}
