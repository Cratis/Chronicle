// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_monitor_startup_fails_after_a_connected_handler_fails : given.a_connection_with_several_endpoints
{
    readonly TimeoutException _failure = new("handler failed");
    Exception _error;

    void Establish()
    {
        _streams[_a] = Healthy;
        _lifecycle.Connected().Returns(Task.FromException(_failure));
        _isConnected = false;
        _tasks.Run(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("monitor failed"));
    }

    async Task Because() => _error = await Catch.Exception(() => StartConnecting().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System));

    [Fact] void should_preserve_the_original_handler_failure() => _error.ShouldBeSame(_failure);
    [Fact] void should_attempt_to_start_monitoring() => _tasks.Received(1).Run(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_try_another_endpoint() => TriedInOrder.ShouldEqual("node-a");
}
