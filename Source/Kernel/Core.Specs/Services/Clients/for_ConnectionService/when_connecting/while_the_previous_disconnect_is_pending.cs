// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

public class while_the_previous_disconnect_is_pending : given.a_gated_connection_service
{
    bool _reconnectWaited;
    ConnectionClaim _newClaim;

    async Task Because()
    {
        var disconnecting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finishDisconnect = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reconnectWaiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _clients.OnClientDisconnected(Arg.Any<ConnectionId>(), Arg.Any<string>()).Returns(_ =>
        {
            disconnecting.TrySetResult();
            return finishDisconnect.Task;
        });
        var oldCancellation = new CancellationTokenSource();
        var old = Start(oldCancellation);
        await old.Connected.WaitAsync(TimeSpan.FromSeconds(5));
        oldCancellation.Cancel();
        await disconnecting.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _ownership.Entering = () => reconnectWaiting.TrySetResult();
        var next = Start(new CancellationTokenSource());
        try
        {
            await reconnectWaiting.Task.WaitAsync(TimeSpan.FromSeconds(5));
            _reconnectWaited = !next.Connected.IsCompleted;
        }
        finally
        {
            finishDisconnect.TrySetResult();
        }

        await old.Completed.WaitAsync(TimeSpan.FromSeconds(5));
        await next.Connected.WaitAsync(TimeSpan.FromSeconds(5));
        _newClaim = _ownership.GetClaim("connection")!;
    }

    [Fact] void should_wait_for_old_disconnect_before_registering() => _reconnectWaited.ShouldBeTrue();
    [Fact] void should_leave_the_new_claim_current() => _ownership.IsCurrent("connection", _newClaim).ShouldBeTrue();
    [Fact] void should_leave_the_new_connection_open() => _openConnections.Responded("connection", false, DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1), out _).ShouldBeTrue();
}
