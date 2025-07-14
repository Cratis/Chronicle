// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.InProcess.for_InProcessConnectionLifecycle;

public class when_registering_event_handlers : Specification
{
    InProcessConnectionLifecycle _inProcessLifecycle;
    ConnectionLifecycle _regularLifecycle;
    int _inProcessCallCount;
    int _regularCallCount;

    void Establish()
    {
        _inProcessLifecycle = new();
        _regularLifecycle = new(NullLogger<ConnectionLifecycle>.Instance);

        _inProcessLifecycle.OnConnected += () =>
        {
            _inProcessCallCount++;
            return Task.CompletedTask;
        };

        _regularLifecycle.OnConnected += () =>
        {
            _regularCallCount++;
            return Task.CompletedTask;
        };
    }

    async Task Because()
    {
        await _inProcessLifecycle.Connected();
        await _regularLifecycle.Connected();
    }

    [Fact] void should_not_trigger_events_for_in_process_lifecycle() => _inProcessCallCount.ShouldEqual(0);
    [Fact] void should_trigger_events_for_regular_lifecycle() => _regularCallCount.ShouldEqual(1);
    [Fact] void should_mark_both_as_connected() => (_inProcessLifecycle.IsConnected && _regularLifecycle.IsConnected).ShouldBeTrue();
}
