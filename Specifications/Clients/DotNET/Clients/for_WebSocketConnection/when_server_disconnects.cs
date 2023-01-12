// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Websocket.Client;

namespace Aksio.Cratis.Clients;

public class when_server_disconnects : given.a_connected_websocket_connection
{
    void Because() => disconnection.OnNext(new DisconnectionInfo(DisconnectionType.Lost, null, string.Empty, string.Empty, null));

    [Fact] void should_be_disconnected() => connection.IsConnected.ShouldBeFalse();
    [Fact] void should_notify_about_disconnect() => client_lifecycle.Verify(_ => _.Disconnected(), Once);
}
