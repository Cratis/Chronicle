// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Websocket.Client;

namespace Aksio.Cratis.Clients;

public class when_connecting_successfully : given.a_websocket_connection
{
    async Task Because()
    {
        received.OnNext(ResponseMessage.TextMessage("kernel-connected"));
        await connection.Connect();
    }

    [Fact] void should_be_connected() => connection.IsConnected.ShouldBeTrue();
}
