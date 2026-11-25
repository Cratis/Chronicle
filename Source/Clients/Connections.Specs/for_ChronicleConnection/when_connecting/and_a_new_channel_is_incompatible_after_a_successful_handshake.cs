// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_a_new_channel_is_incompatible_after_a_successful_handshake : given.a_connection_after_a_successful_handshake
{
    Exception _error;

    async Task Because()
    {
        _isConnected = false;
        _compatibility[_a] = () => Task.FromResult(new CompatibilityResponse { IsCompatible = false });
        _error = await Catch.Exception(() => StartConnecting().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System));
        _compatibility.Clear();
        await TickWatchdog();
    }

    [Fact] void should_surface_the_incompatibility() => _error.ShouldBeOfExactType<IncompatibleServerException>();
    [Fact] void should_not_refresh_monitoring_using_the_previous_handshake() => _disconnectedCalls.ShouldEqual(1);
    [Fact] void should_reconnect_on_the_next_monitor_tick() => _servedBy.Count.ShouldEqual(2);
    [Fact] void should_not_start_another_monitor() => _tasks.Received(1).Run(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>());
}
