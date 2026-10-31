// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ClientConnection.when_disconnecting;

public class and_connected : given.a_client_connection
{
    async Task Establish() => await _connection.Connect();

    Task Because() => _connection.Disconnect();

    [Fact] void should_release_after_having_pinned() => (_connection.PinCount, _connection.ReleaseCount).ShouldEqual((1, 1));
}
