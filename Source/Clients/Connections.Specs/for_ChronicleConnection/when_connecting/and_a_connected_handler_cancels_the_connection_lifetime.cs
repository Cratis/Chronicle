// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_a_connected_handler_cancels_the_connection_lifetime : given.a_connection_with_several_endpoints
{
    readonly InvalidOperationException _failure = new("handler failed during shutdown");
    Exception _error;

    void Establish()
    {
        _streams[_a] = Healthy;
        _lifecycle.Connected().Returns(async _ =>
        {
            await _cancellation.CancelAsync();
            throw _failure;
        });
        _isConnected = false;
    }

    async Task Because() => _error = await Catch.Exception(() => StartConnecting().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System));

    [Fact] void should_preserve_the_handler_failure() => _error.ShouldBeSame(_failure);
    [Fact] void should_not_start_a_monitor_after_shutdown() => _tasks.DidNotReceive().Run(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>());
}
