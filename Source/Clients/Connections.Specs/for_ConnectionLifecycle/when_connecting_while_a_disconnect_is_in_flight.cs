// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ConnectionLifecycle;

public class when_connecting_while_a_disconnect_is_in_flight : Specification
{
    readonly SignallingLifecycleLogger _logger = new();
    readonly TaskCompletionSource _disconnectEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseDisconnect = new(TaskCreationOptions.RunContinuationsAsynchronously);
    TaskCompletionSource _probeRan = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ConnectionLifecycle _lifecycle;
    ConnectionId _before;
    bool _disconnectCompletedWhenProbeRan;
    ConnectionId _connectionIdWhenProbeRan;
    bool _connectedWhileFenced;

    void Establish()
    {
        _lifecycle = new ConnectionLifecycle(_logger);
        _lifecycle.OnDisconnected += async () =>
        {
            _disconnectEntered.TrySetResult();
            await _releaseDisconnect.Task;
        };
        _lifecycle.OnConnected += () =>
        {
            _disconnectCompletedWhenProbeRan = _releaseDisconnect.Task.IsCompleted;
            _connectionIdWhenProbeRan = _lifecycle.ConnectionId;
            _probeRan.TrySetResult();
            return Task.CompletedTask;
        };
    }

    async Task Because()
    {
        await _lifecycle.Connected();
        _probeRan = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _before = _lifecycle.ConnectionId;
        var disconnecting = _lifecycle.Disconnected();
        await _disconnectEntered.Task;
        var connecting = _lifecycle.Connected();

        // Whichever happens first releases the disconnect, so the spec finishes on broken code too and the facts report it.
        await Task.WhenAny(_logger.Fenced, _probeRan.Task);
        _connectedWhileFenced = _lifecycle.IsConnected;
        _releaseDisconnect.TrySetResult();
        await Task.WhenAll(disconnecting, connecting);
    }

    [Fact] void should_complete_the_disconnect_handlers_before_the_connected_handlers_run() => _disconnectCompletedWhenProbeRan.ShouldBeTrue();
    [Fact] void should_rotate_the_connection_id_before_the_connected_handlers_run() => _connectionIdWhenProbeRan.ShouldNotEqual(_before);
    [Fact] void should_not_be_connected_while_fenced() => _connectedWhileFenced.ShouldBeFalse();
    [Fact] void should_be_connected_afterwards() => _lifecycle.IsConnected.ShouldBeTrue();
}
