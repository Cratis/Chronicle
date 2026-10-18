// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_getting_services;

public class and_the_connect_never_completes : given.a_connection_that_lost_a_working_kernel
{
    const int CancelAfterMilliseconds = 50;

    Exception _error;
    TimeSpan _elapsed;
    TimeSpan _timeToReturnControl;

    void Establish() => _serverAddressResolver
        .Resolve(Arg.Any<ChronicleConnectionString>())
        .Returns(_ =>
        {
            _resolveAttempts++;
            return new TaskCompletionSource<IReadOnlyList<ChronicleServerAddress>>().Task;
        });

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource(CancelAfterMilliseconds);
        var stopwatch = Stopwatch.StartNew();
        _error = await Catch.Exception(async () =>
        {
            var pending = _connection.GetServices(cancellation.Token);
            _timeToReturnControl = stopwatch.Elapsed;
            await pending;
        });
        _elapsed = stopwatch.Elapsed;
    }

    [Fact] void should_throw_operation_cancelled() => _error.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_return_control_to_the_caller_without_blocking() => _timeToReturnControl.ShouldBeLessThan(TimeSpan.FromSeconds(ConnectTimeoutSeconds) / 2);
    [Fact] void should_cancel_well_before_the_connect_timeout() => _elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(ConnectTimeoutSeconds) / 2);
    [Fact] void should_have_started_a_connect_attempt() => _resolveAttempts.ShouldEqual(1);
}
