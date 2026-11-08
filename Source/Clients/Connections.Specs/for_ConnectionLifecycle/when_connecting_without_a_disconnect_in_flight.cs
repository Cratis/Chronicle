// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ConnectionLifecycle;

public class when_connecting_without_a_disconnect_in_flight : Specification
{
    readonly SignallingLifecycleLogger _logger = new();
    ConnectionLifecycle _lifecycle;
    int _handlersRan;

    void Establish()
    {
        _lifecycle = new ConnectionLifecycle(_logger);
        _lifecycle.OnConnected += () =>
        {
            _handlersRan++;
            return Task.CompletedTask;
        };
    }

    Task Because() => _lifecycle.Connected();

    [Fact] void should_not_wait_for_a_disconnect() => _logger.Fenced.IsCompleted.ShouldBeFalse();
    [Fact] void should_run_the_connected_handlers() => _handlersRan.ShouldEqual(1);
    [Fact] void should_be_connected() => _lifecycle.IsConnected.ShouldBeTrue();
}
