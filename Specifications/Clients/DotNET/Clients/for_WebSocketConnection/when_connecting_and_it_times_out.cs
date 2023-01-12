// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Clients;

public class when_connecting_and_it_times_out : given.a_websocket_connection
{
    Exception result;

    void Establish()
    {
        timer_factory.Setup(_ => _.Create(IsAny<TimerCallback>(), WebSocketConnection.ConnectTimeout, Timeout.Infinite, null))
            .Returns(
                (TimerCallback callback, int _, int __, object? ___) =>
                {
                    callback(null);
                    return timeout_timer.Object;
                });
    }

    async Task Because() => result = await Catch.Exception(async () => await connection.Connect());

    [Fact] void should_throw_connection_timed_out() => result.ShouldBeOfExactType<ConnectionTimedOut>();
}
