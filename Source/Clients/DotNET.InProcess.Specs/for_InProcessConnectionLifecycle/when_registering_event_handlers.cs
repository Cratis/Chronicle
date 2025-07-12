// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.InProcess.for_InProcessConnectionLifecycle;

public class when_registering_event_handlers : Specification
{
    InProcessConnectionLifecycle inProcessLifecycle;
    ConnectionLifecycle regularLifecycle;
    int inProcessCallCount;
    int regularCallCount;

    void Establish()
    {
        inProcessLifecycle = new();
        regularLifecycle = new(NullLogger<ConnectionLifecycle>.Instance);

        inProcessLifecycle.OnConnected += () =>
        {
            inProcessCallCount++;
            return Task.CompletedTask;
        };

        regularLifecycle.OnConnected += () =>
        {
            regularCallCount++;
            return Task.CompletedTask;
        };
    }

    async Task Because()
    {
        await inProcessLifecycle.Connected();
        await regularLifecycle.Connected();
    }

    [Fact] void should_not_trigger_events_for_in_process_lifecycle() => inProcessCallCount.ShouldEqual(0);
    [Fact] void should_trigger_events_for_regular_lifecycle() => regularCallCount.ShouldEqual(1);
    [Fact] void should_mark_both_as_connected() => (inProcessLifecycle.IsConnected && regularLifecycle.IsConnected).ShouldBeTrue();
}
