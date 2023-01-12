// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Clients;

public class when_disposing_after_connect : given.a_connected_websocket_connection
{
    void Because() => connection.Dispose();

    [Fact] void should_send_disconnect() => messages.Last().ShouldEqual("disconnect");
    [Fact] void should_dispose_timer() => timeout_timer.Verify(_ => _.Dispose(), Once);
}
