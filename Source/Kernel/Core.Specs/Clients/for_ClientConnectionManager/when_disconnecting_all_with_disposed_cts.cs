// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ClientConnectionManager;

public class when_disconnecting_all_with_disposed_cts : given.a_connection_manager
{
    CancellationTokenSource _disposedCts;
    CancellationTokenSource _activeCts;

    void Establish()
    {
        _disposedCts = new CancellationTokenSource();
        _disposedCts.Dispose();
        _activeCts = new CancellationTokenSource();
        _manager.Register(NewConnectionId(), _disposedCts);
        _manager.Register(NewConnectionId(), _activeCts);
    }

    void Because() => _manager.DisconnectAll("test reset with disposed");

    [Fact] void should_not_throw() => true.ShouldBeTrue();
    [Fact] void should_cancel_active_connection() => _activeCts.IsCancellationRequested.ShouldBeTrue();
}
