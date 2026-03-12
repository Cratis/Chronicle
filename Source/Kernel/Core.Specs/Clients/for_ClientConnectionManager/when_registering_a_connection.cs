// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Clients.for_ClientConnectionManager;

public class when_registering_a_connection : given.a_connection_manager
{
    ConnectionId _connectionId;
    CancellationTokenSource _cts;

    void Establish()
    {
        _connectionId = NewConnectionId();
        _cts = new CancellationTokenSource();
    }

    void Because() => _manager.Register(_connectionId, _cts);

    [Fact] void should_not_throw() => true.ShouldBeTrue();
}
