// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_keeping_a_connection_alive;

public class and_the_connection_is_healthy : given.a_connected_client
{
    async Task Because()
    {
        Connect();
        await WaitUntil(() => KeepAlivesSent >= 1);
        await Respond();
        await WaitUntil(() => KeepAlivesSent >= 2);
    }

    [Fact] void should_not_complete_the_stream() => Completed.ShouldBeFalse();
    [Fact] void should_keep_sending_keep_alives() => KeepAlivesSent.ShouldBeGreaterThanOrEqual(2);
}
