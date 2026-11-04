// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Connections.for_ExecutionContextDetached;

public class when_starting_a_task_fails : Specification
{
    readonly AsyncLocal<string> _ambient = new();
    readonly InvalidOperationException _failure = new("startup failed");
    Activity _activity;
    Exception _exception;
    string _callerAmbient;
    Activity _callerActivity;
    bool _flowSuppressed;

    void Because()
    {
        _ambient.Value = "request-tenant";
        _activity = new Activity("request").Start();
        try
        {
            _ = ExecutionContextDetached.Start(() => throw _failure);
        }
        catch (InvalidOperationException exception)
        {
            _exception = exception;
        }
        _callerAmbient = _ambient.Value;
        _callerActivity = Activity.Current;
        _flowSuppressed = ExecutionContext.IsFlowSuppressed();
    }

    void Destroy() => _activity?.Dispose();

    [Fact] void should_surface_the_startup_failure() => _exception.ShouldBeSame(_failure);
    [Fact] void should_preserve_the_callers_ambient_state() => _callerAmbient.ShouldEqual("request-tenant");
    [Fact] void should_preserve_the_callers_activity() => _callerActivity.ShouldBeSame(_activity);
    [Fact] void should_restore_the_callers_flow() => _flowSuppressed.ShouldBeFalse();
}
