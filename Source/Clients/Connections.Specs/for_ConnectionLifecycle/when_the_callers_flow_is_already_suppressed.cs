// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ConnectionLifecycle;

public class when_the_callers_flow_is_already_suppressed : given.a_lifecycle_with_ambient_state
{
    bool _stillSuppressed;

    void Establish() => _lifecycle.OnConnected += Handle;

    async Task Because()
    {
        SetCallerState();
        Task connected;
        using (ExecutionContext.SuppressFlow())
        {
            connected = _lifecycle.Connected();
            _stillSuppressed = ExecutionContext.IsFlowSuppressed();
        }
        await connected;
        RecordCallerState();
    }

    [Fact] void should_run_the_handler_without_the_callers_ambient_state() => _handlerAmbient.ShouldBeNull();
    [Fact] void should_run_the_handler_without_the_callers_activity() => _handlerActivity.ShouldBeNull();
    [Fact] void should_leave_flow_suppressed_inside_the_callers_scope() => _stillSuppressed.ShouldBeTrue();
    [Fact] void should_preserve_the_callers_ambient_state() => _callerAmbient.ShouldEqual("request-tenant");
    [Fact] void should_restore_the_callers_flow_after_the_scope() => _callerFlowSuppressed.ShouldBeFalse();
}
