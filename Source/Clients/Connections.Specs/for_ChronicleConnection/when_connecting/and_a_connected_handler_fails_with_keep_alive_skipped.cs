// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_a_connected_handler_fails_with_keep_alive_skipped : given.a_connection_with_several_endpoints
{
    readonly InvalidOperationException _failure = new("handler failed");
    Exception _error;

    protected override bool SkipKeepAlive => true;

    void Establish()
    {
        _lifecycle.Connected().Returns(Task.FromException(_failure));
        _isConnected = false;
    }

    async Task Because() => _error = await Catch.Exception(() => StartConnecting().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System));

    [Fact] void should_preserve_the_handler_failure() => _error.ShouldBeSame(_failure);
    [Fact] void should_not_start_a_monitor() => _tasks.DidNotReceive().Run(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_create_a_keep_alive_stream() => _servedBy.ShouldBeEmpty();
}
