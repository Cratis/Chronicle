// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.InProcess.for_InProcessConnectionLifecycle;

public class when_connected : Specification
{
    InProcessConnectionLifecycle _lifecycle;
    bool _onConnectedCalled;

    void Establish()
    {
        _lifecycle = new();
        _lifecycle.OnConnected += () =>
        {
            _onConnectedCalled = true;
            return Task.CompletedTask;
        };
    }

    async Task Because() => await _lifecycle.Connected();

    [Fact] void should_mark_as_connected() => _lifecycle.IsConnected.ShouldBeTrue();
    [Fact] void should_not_trigger_on_connected_events() => _onConnectedCalled.ShouldBeFalse();
}
