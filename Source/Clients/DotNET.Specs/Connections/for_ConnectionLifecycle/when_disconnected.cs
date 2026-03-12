// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_ConnectionLifecycle;

public class when_disconnected : Specification
{
    ConnectionLifecycle _lifecycle;
    bool _onDisconnectedCalled;
    ConnectionId _connectionIdBeforeDisconnect;

    void Establish()
    {
        _lifecycle = new ConnectionLifecycle(NullLogger<ConnectionLifecycle>.Instance);
        _lifecycle.OnDisconnected += () =>
        {
            _onDisconnectedCalled = true;
            return Task.CompletedTask;
        };

        // Must be connected first before disconnecting
        _lifecycle.Connected().GetAwaiter().GetResult();
        _connectionIdBeforeDisconnect = _lifecycle.ConnectionId;
    }

    async Task Because() => await _lifecycle.Disconnected();

    [Fact] void should_mark_as_not_connected() => _lifecycle.IsConnected.ShouldBeFalse();
    [Fact] void should_trigger_on_disconnected_event() => _onDisconnectedCalled.ShouldBeTrue();
    [Fact] void should_generate_new_connection_id() => _lifecycle.ConnectionId.ShouldNotEqual(_connectionIdBeforeDisconnect);
}
