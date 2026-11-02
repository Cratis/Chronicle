// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_keeping_a_connection_alive;

public class and_the_call_ends_while_checking_the_registry_for_a_client_running_with_a_debugger : given.a_connected_client
{
    readonly TaskCompletionSource<bool> _isConnected = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _completedWhileTheRegistryWasPending;

    void Establish() => _connectedClients.IsConnected(new ConnectionId(TheConnection)).Returns(_isConnected.Task);

    async Task Because()
    {
        Connect(isRunningWithDebugger: true);
        await WaitUntil(() => CallsTo(nameof(IConnectedClients.IsConnected)) >= 1);
        await Cancel();
        await WaitUntil(() => Completed);
        _completedWhileTheRegistryWasPending = Completed && !_isConnected.Task.IsCompleted;

        // Settle the test-owned registry call the service gave up on.
        _isConnected.TrySetResult(true);
    }

    [Fact] void should_complete_without_waiting_for_the_registry() => _completedWhileTheRegistryWasPending.ShouldBeTrue();
    [Fact] Task should_not_put_the_client_back() => WasRestored(0);
}
