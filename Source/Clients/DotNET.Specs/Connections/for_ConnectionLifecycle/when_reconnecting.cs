// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_ConnectionLifecycle;

public class when_reconnecting : Specification
{
    ConnectionLifecycle _lifecycle;
    int _connectedCount;
    int _disconnectedCount;

    void Establish()
    {
        _lifecycle = new ConnectionLifecycle(NullLogger<ConnectionLifecycle>.Instance);
        _lifecycle.OnConnected += () =>
        {
            _connectedCount++;
            return Task.CompletedTask;
        };
        _lifecycle.OnDisconnected += () =>
        {
            _disconnectedCount++;
            return Task.CompletedTask;
        };
    }

    async Task Because()
    {
        await _lifecycle.Connected();
        await _lifecycle.Disconnected();
        await _lifecycle.Connected();
    }

    [Fact] void should_be_connected() => _lifecycle.IsConnected.ShouldBeTrue();
    [Fact] void should_have_called_connected_twice() => _connectedCount.ShouldEqual(2);
    [Fact] void should_have_called_disconnected_once() => _disconnectedCount.ShouldEqual(1);
}
