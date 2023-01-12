// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Websocket.Client;
using Websocket.Client.Models;

namespace Aksio.Cratis.Clients;

public class when_reconnection_happened : given.a_connected_websocket_connection
{
    void Because() => reconnection.OnNext(new ReconnectionInfo(ReconnectionType.ByServer));

    [Fact] void should_reconnect_with_client_information() => client_information.Count.ShouldEqual(2);
    [Fact] void should_have_different_connection_id_for_reconnect() => client_information[1].ConnectionId.ShouldNotEqual(client_information[0].ConnectionId);
}
