// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClientStartup;

public class when_waiting_for_completion_and_the_host_stops_before_starting : given.a_chronicle_client_startup
{
    bool _completed;

    async Task Because()
    {
        await _startup.StartAsync(CancellationToken.None);
        await _startup.StopAsync(CancellationToken.None);
        await _startup.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        _completed = _startup.Completion.IsCompletedSuccessfully;
    }

    [Fact] void should_complete() => _completed.ShouldBeTrue();
    [Fact] void should_not_connect() => _connection.DidNotReceive().Connect();
}
