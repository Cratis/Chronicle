// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_getting_services;

public class and_the_connect_never_completes : given.a_connection_that_lost_a_working_kernel
{
    bool _completedBeforeCancellation;
    Exception _error;

    void Establish() => _serverAddressResolver
        .Resolve(Arg.Any<ChronicleConnectionString>())
        .Returns(_ =>
        {
            _resolveAttempts++;
            return new TaskCompletionSource<IReadOnlyList<ChronicleServerAddress>>().Task;
        });

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        var pending = _connection.GetServices(cancellation.Token).AsTask();
        _completedBeforeCancellation = pending.IsCompleted;
        await cancellation.CancelAsync();
        _error = await Catch.Exception(() => pending);
    }

    [Fact] void should_return_control_while_the_connect_is_pending() => _completedBeforeCancellation.ShouldBeFalse();
    [Fact] void should_throw_operation_cancelled() => _error.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_have_started_a_connect_attempt() => _resolveAttempts.ShouldEqual(1);
}
