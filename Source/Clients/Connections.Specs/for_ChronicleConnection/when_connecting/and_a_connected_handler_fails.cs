// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_a_connected_handler_fails : given.a_connection_with_ambient_state
{
    readonly InvalidOperationException _failure = new("handler failed");
    Exception _exception;
    string _callerAmbient;
    Activity _callerActivity;
    bool _flowSuppressed;

    void Establish()
    {
        _lifecycle.Connected().Returns(Task.FromException(_failure));
        _isConnected = false;
    }

    async Task Because()
    {
        _ambient.Value = "request-tenant";
        _activity = new Activity("request").Start();
        try
        {
            await _connection.Connect();
        }
        catch (InvalidOperationException exception)
        {
            _exception = exception;
        }
        _callerAmbient = _ambient.Value;
        _callerActivity = Activity.Current;
        _flowSuppressed = ExecutionContext.IsFlowSuppressed();
    }

    [Fact] void should_surface_the_handler_failure_to_the_connect_awaiter() => _exception.ShouldBeSame(_failure);
    [Fact] void should_preserve_the_callers_ambient_state() => _callerAmbient.ShouldEqual("request-tenant");
    [Fact] void should_preserve_the_callers_activity() => _callerActivity.ShouldBeSame(_activity);
    [Fact] void should_restore_the_callers_flow() => _flowSuppressed.ShouldBeFalse();
}
