// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_getting_services;

public class and_reconnecting_from_a_request_with_ambient_state : given.a_connection_that_lost_a_working_kernel
{
    readonly AsyncLocal<string> _ambient = new();
    Activity _activity;
    string _connectedAmbient;
    Activity _connectedActivity;
    string _callerAmbient;
    Activity _callerActivity;
    bool _callerFlowSuppressed;

    async Task Establish()
    {
        await _connection.Connect();
        _lifecycle.Connected().Returns(_ =>
        {
            _connectedAmbient = _ambient.Value;
            _connectedActivity = Activity.Current;
            _isConnected = true;
            return Task.CompletedTask;
        });
        _isConnected = false;
    }

    void Because()
    {
        _ambient.Value = "request-tenant";
        _activity = new Activity("request").Start();
        _ = ((IChronicleServicesAccessor)_connection).Services;
        _callerAmbient = _ambient.Value;
        _callerActivity = Activity.Current;
        _callerFlowSuppressed = ExecutionContext.IsFlowSuppressed();
    }

    void Destroy() => _activity?.Dispose();

    [Fact] void should_not_pass_the_callers_ambient_state_to_connected() => _connectedAmbient.ShouldBeNull();
    [Fact] void should_not_pass_the_callers_activity_to_connected() => _connectedActivity.ShouldBeNull();
    [Fact] void should_preserve_the_callers_ambient_state() => _callerAmbient.ShouldEqual("request-tenant");
    [Fact] void should_preserve_the_callers_activity() => _callerActivity.ShouldBeSame(_activity);
    [Fact] void should_restore_the_callers_flow() => _callerFlowSuppressed.ShouldBeFalse();
}
