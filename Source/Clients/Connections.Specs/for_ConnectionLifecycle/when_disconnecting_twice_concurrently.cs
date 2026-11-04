// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ConnectionLifecycle;

public class when_disconnecting_twice_concurrently : Specification
{
    readonly SignallingLifecycleLogger _logger = new();
    readonly TaskCompletionSource _releaseDisconnects = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ConnectionLifecycle _lifecycle;
    int _disconnectsEntered;
    bool _disconnectsCompletedWhenConnected;
    int _disconnectsEnteredWhenConnected;

    void Establish()
    {
        _lifecycle = new ConnectionLifecycle(_logger);
        _lifecycle.OnDisconnected += async () =>
        {
            Interlocked.Increment(ref _disconnectsEntered);
            await _releaseDisconnects.Task;
        };
        _lifecycle.OnConnected += () =>
        {
            _disconnectsCompletedWhenConnected = _releaseDisconnects.Task.IsCompleted;
            _disconnectsEnteredWhenConnected = _disconnectsEntered;
            return Task.CompletedTask;
        };
    }

    async Task Because()
    {
        var first = _lifecycle.Disconnected();
        var second = _lifecycle.Disconnected();
        var connecting = _lifecycle.Connected();
        await _logger.Fenced;
        _releaseDisconnects.SetResult();
        await Task.WhenAll(first, second, connecting);
    }

    [Fact] void should_run_both_disconnects() => _disconnectsEnteredWhenConnected.ShouldEqual(2);
    [Fact] void should_wait_for_both_before_connecting() => _disconnectsCompletedWhenConnected.ShouldBeTrue();
    [Fact] void should_be_connected_afterwards() => _lifecycle.IsConnected.ShouldBeTrue();
}
