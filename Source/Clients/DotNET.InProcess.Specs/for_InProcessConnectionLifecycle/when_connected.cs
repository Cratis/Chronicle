// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.InProcess.for_InProcessConnectionLifecycle;

public class when_connected : Specification
{
    InProcessConnectionLifecycle lifecycle;
    bool onConnectedCalled;

    void Establish()
    {
        lifecycle = new();
        lifecycle.OnConnected += () =>
        {
            onConnectedCalled = true;
            return Task.CompletedTask;
        };
    }

    async Task Because() => await lifecycle.Connected();

    [Fact] void should_mark_as_connected() => lifecycle.IsConnected.ShouldBeTrue();
    [Fact] void should_not_trigger_on_connected_events() => onConnectedCalled.ShouldBeFalse();
}
