// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ClientConnectionManager;

public class when_blocking_connections : given.a_connection_manager
{
    Task _waitTask;

    void Establish() => _manager.BlockConnections();

    void Because() => _waitTask = _manager.WaitUntilAcceptingConnections();

    [Fact] void should_block_waiting_callers() => _waitTask.IsCompleted.ShouldBeFalse();
}
