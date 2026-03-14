// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ClientConnectionManager;

public class when_disconnecting_all : given.a_connection_manager
{
    CancellationTokenSource _firstCts;
    CancellationTokenSource _secondCts;

    void Establish()
    {
        _firstCts = new CancellationTokenSource();
        _secondCts = new CancellationTokenSource();
        _manager.Register(NewConnectionId(), _firstCts);
        _manager.Register(NewConnectionId(), _secondCts);
    }

    void Because() => _manager.DisconnectAll("test reset");

    [Fact] void should_cancel_first_connection() => _firstCts.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_cancel_second_connection() => _secondCts.IsCancellationRequested.ShouldBeTrue();
}
