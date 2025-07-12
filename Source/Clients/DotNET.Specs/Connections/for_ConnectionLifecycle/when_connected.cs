// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_ConnectionLifecycle;

public class when_connected : Specification
{
    ConnectionLifecycle lifecycle;
    bool onConnectedCalled;

    void Establish()
    {
        lifecycle = new ConnectionLifecycle(NullLogger<ConnectionLifecycle>.Instance);
        lifecycle.OnConnected += () =>
        {
            onConnectedCalled = true;
            return Task.CompletedTask;
        };
    }

    async Task Because() => await lifecycle.Connected();

    [Fact] void should_mark_as_connected() => lifecycle.IsConnected.ShouldBeTrue();
    [Fact] void should_trigger_on_connected_events() => onConnectedCalled.ShouldBeTrue();
}
