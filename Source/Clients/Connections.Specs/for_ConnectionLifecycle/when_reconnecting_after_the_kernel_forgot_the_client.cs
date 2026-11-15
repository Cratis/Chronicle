// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ConnectionLifecycle;

public class when_reconnecting_after_the_kernel_forgot_the_client : given.a_lifecycle_with_ambient_state
{
    ConnectionId _staleConnectionId;
    ConnectionId _idSeenByHandlersWhileDisconnecting;

    void Establish()
    {
        _staleConnectionId = _lifecycle.ConnectionId;
        _lifecycle.OnDisconnected += () =>
        {
            _idSeenByHandlersWhileDisconnecting = _lifecycle.ConnectionId;
            return Task.CompletedTask;
        };
    }

    async Task Because()
    {
        await _lifecycle.Connected();
        await _lifecycle.Disconnected();
    }

    [Fact] void should_let_handlers_tear_down_with_the_stale_connection_id() => _idSeenByHandlersWhileDisconnecting.ShouldEqual(_staleConnectionId);
    [Fact] void should_issue_a_new_connection_id() => _lifecycle.ConnectionId.ShouldNotEqual(_staleConnectionId);
    [Fact] void should_not_be_connected() => _lifecycle.IsConnected.ShouldBeFalse();
}
