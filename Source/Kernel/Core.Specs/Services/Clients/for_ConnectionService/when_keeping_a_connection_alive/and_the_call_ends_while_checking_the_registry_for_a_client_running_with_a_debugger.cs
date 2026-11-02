// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_keeping_a_connection_alive;

public class and_the_call_ends_while_checking_the_registry_for_a_client_running_with_a_debugger : given.a_connected_client
{
    readonly TaskCompletionSource<bool> _pinged = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _completedWhileTheRegistryWasPending;
    bool _disconnectedBeforeTheCheckSettled;

    void Establish() => _connectedClients.OnClientPing(new ConnectionId(TheConnection)).Returns(_pinged.Task);

    async Task Because()
    {
        Connect(isRunningWithDebugger: true);
        await WaitUntil(() => CallsTo(nameof(IConnectedClients.OnClientPing)) >= 1);
        await Cancel();
        await WaitUntil(() => Completed);
        _completedWhileTheRegistryWasPending = Completed && !_pinged.Task.IsCompleted;
        _disconnectedBeforeTheCheckSettled = CallsTo(nameof(IConnectedClients.OnClientDisconnected)) > 0;

        // The registry answers late that it no longer knows the client.
        _pinged.TrySetResult(false);
        await WaitUntil(() => CallsTo(nameof(IConnectedClients.OnClientDisconnected)) >= 1);
    }

    [Fact] void should_complete_without_waiting_for_the_registry() => _completedWhileTheRegistryWasPending.ShouldBeTrue();
    [Fact] void should_not_disconnect_before_the_check_settles() => _disconnectedBeforeTheCheckSettled.ShouldBeFalse();
    [Fact] Task should_not_put_the_client_back_after_the_call_ended() => WasRestored(0);
    [Fact] void should_disconnect_once() => CallsTo(nameof(IConnectedClients.OnClientDisconnected)).ShouldEqual(1);
}
