// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ClientConnectionManager;

public class when_waiting_and_gate_is_open : given.a_connection_manager
{
    Task _waitTask;

    void Because() => _waitTask = _manager.WaitUntilAcceptingConnections();

    [Fact] void should_complete_immediately() => _waitTask.IsCompleted.ShouldBeTrue();
}
