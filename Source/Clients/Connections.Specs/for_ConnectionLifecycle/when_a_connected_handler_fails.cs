// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ConnectionLifecycle;

public class when_a_connected_handler_fails : given.a_lifecycle_with_ambient_state
{
    readonly InvalidOperationException _failure = new("handler failed");
    AggregateException _exception;

    void Establish() => _lifecycle.OnConnected += () => throw _failure;

    async Task Because()
    {
        SetCallerState();
        try
        {
            await _lifecycle.Connected();
        }
        catch (AggregateException exception)
        {
            _exception = exception;
        }
        RecordCallerState();
    }

    [Fact] void should_surface_the_same_handler_exception() => _exception.InnerExceptions.Single().ShouldBeSame(_failure);
    [Fact] void should_roll_back_the_connected_state() => _lifecycle.IsConnected.ShouldBeFalse();
    [Fact] void should_preserve_the_callers_ambient_state() => _callerAmbient.ShouldEqual("request-tenant");
    [Fact] void should_restore_the_callers_flow() => _callerFlowSuppressed.ShouldBeFalse();
}
