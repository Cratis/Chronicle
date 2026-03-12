// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ClientConnectionManager;

public class when_blocking_then_allowing_connections : given.a_connection_manager
{
    Task _waitTask;
    bool _completed;

    void Establish()
    {
        _manager.BlockConnections();
        _waitTask = Task.Run(async () =>
        {
            await _manager.WaitUntilAcceptingConnections();
            _completed = true;
        });
    }

    void Because() => _manager.AllowConnections();

    [Fact] void should_release_waiting_callers()
    {
        _waitTask.Wait(TimeSpan.FromSeconds(2));
        _completed.ShouldBeTrue();
    }
}
