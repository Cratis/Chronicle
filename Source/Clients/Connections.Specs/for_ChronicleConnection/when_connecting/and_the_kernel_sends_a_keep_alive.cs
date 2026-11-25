// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_the_kernel_sends_a_keep_alive : given.a_connection_with_several_endpoints
{
    async Task Because()
    {
        _streams[_a] = Healthy;
        await StartConnecting().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
    }

    [Fact] void should_answer_the_keep_alive() => _keepAlivesAnswered.ShouldEqual(1);
}
