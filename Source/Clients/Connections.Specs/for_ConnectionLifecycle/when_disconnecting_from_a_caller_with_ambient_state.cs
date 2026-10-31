// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ConnectionLifecycle;

public class when_disconnecting_from_a_caller_with_ambient_state : given.a_lifecycle_with_ambient_state
{
    void Establish() => _lifecycle.OnDisconnected += Handle;

    async Task Because()
    {
        await _lifecycle.Connected();
        SetCallerState();
        await _lifecycle.Disconnected();
        RecordCallerState();
    }

    [Fact] void should_start_the_handler_without_the_callers_ambient_state() => _handlerAmbient.ShouldBeNull();
    [Fact] void should_start_the_handler_without_the_callers_activity() => _handlerActivity.ShouldBeNull();
    [Fact] void should_continue_the_handler_without_the_callers_ambient_state() => _continuedAmbient.ShouldBeNull();
    [Fact] void should_continue_the_handler_without_the_callers_activity() => _continuedActivity.ShouldBeNull();
    [Fact] void should_preserve_the_callers_ambient_state() => _callerAmbient.ShouldEqual("request-tenant");
    [Fact] void should_preserve_the_callers_activity() => _callerActivity.ShouldBeSame(_activity);
    [Fact] void should_restore_the_callers_flow() => _callerFlowSuppressed.ShouldBeFalse();
    [Fact] void should_be_disconnected() => _lifecycle.IsConnected.ShouldBeFalse();
}
