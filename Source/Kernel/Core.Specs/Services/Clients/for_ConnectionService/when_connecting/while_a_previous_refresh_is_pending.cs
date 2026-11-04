// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

public class while_a_previous_refresh_is_pending : given.a_gated_connection_service
{
    bool _reconnectWaited;
    bool _refreshFinishedInItsGeneration;

    async Task Because()
    {
        var pinging = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finishPing = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _clients.OnClientPing(Arg.Any<ConnectionId>()).Returns(_ =>
        {
            pinging.TrySetResult();
            return finishPing.Task;
        });
        var old = Start(new CancellationTokenSource());
        await old.Connected.WaitAsync(TimeSpan.FromSeconds(5));
        var claim = _ownership.GetClaim("connection")!;
        _clients.OnClientRestored(Arg.Any<ConnectionId>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(_ =>
            {
                _refreshFinishedInItsGeneration = _ownership.IsCurrent("connection", claim);
                return Task.CompletedTask;
            });
        var refresh = _service.ConnectionKeepAlive(new ConnectionKeepAlive { ConnectionId = "connection" });
        await pinging.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _ownership.Entering = () => waiting.TrySetResult();
        var next = Start(new CancellationTokenSource());
        try
        {
            await waiting.Task.WaitAsync(TimeSpan.FromSeconds(5));
            _reconnectWaited = !next.Connected.IsCompleted;
        }
        finally
        {
            finishPing.TrySetResult(false);
        }

        await refresh.WaitAsync(TimeSpan.FromSeconds(5));
        await next.Connected.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_finish_old_refresh_before_replacing_its_claim() => _refreshFinishedInItsGeneration.ShouldBeTrue();
    [Fact] void should_wait_before_registering_the_new_generation() => _reconnectWaited.ShouldBeTrue();
}
