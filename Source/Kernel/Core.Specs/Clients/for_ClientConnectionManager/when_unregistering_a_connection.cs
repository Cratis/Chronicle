// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Clients.for_ClientConnectionManager;

public class when_unregistering_a_connection : given.a_connection_manager
{
    ConnectionId _connectionId;
    CancellationTokenSource _cts;

    void Establish()
    {
        _connectionId = NewConnectionId();
        _cts = new CancellationTokenSource();
        _manager.Register(_connectionId, _cts);
    }

    void Because() => _manager.Unregister(_connectionId);

    [Fact] void should_not_cancel_the_cts() => _cts.IsCancellationRequested.ShouldBeFalse();
    [Fact] void should_not_disconnect_when_disconnect_all_called_later()
    {
        _manager.DisconnectAll("after unregister");
        _cts.IsCancellationRequested.ShouldBeFalse();
    }
}
