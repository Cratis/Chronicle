// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_the_caller_has_ambient_state : given.a_connection_with_ambient_state
{
    string _connectedAmbient;
    Activity _connectedActivity;
    string _callerAmbient;
    Activity _callerActivity;

    void Establish()
    {
        _lifecycle.Connected().Returns(_ =>
        {
            _connectedAmbient = _ambient.Value;
            _connectedActivity = Activity.Current;
            _isConnected = true;
            return Task.CompletedTask;
        });
        _isConnected = false;
    }

    async Task Because()
    {
        _ambient.Value = "request-tenant";
        _activity = new Activity("request").Start();
        await _connection.Connect();
        _callerAmbient = _ambient.Value;
        _callerActivity = Activity.Current;
        await Task.WhenAll(_keepAliveAmbient.Task, _watchdogAmbient.Task).WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_not_pass_the_callers_ambient_state_to_connected() => _connectedAmbient.ShouldBeNull();
    [Fact] void should_not_pass_the_callers_activity_to_connected() => _connectedActivity.ShouldBeNull();
    [Fact] async Task should_not_pass_the_callers_ambient_state_to_the_keep_alive_handler() => (await _keepAliveAmbient.Task).ShouldBeNull();
    [Fact] async Task should_not_pass_the_callers_activity_to_the_keep_alive_handler() => (await _keepAliveActivity.Task).ShouldBeNull();
    [Fact] async Task should_not_pass_the_callers_ambient_state_to_the_watchdog_loop() => (await _watchdogAmbient.Task).ShouldBeNull();
    [Fact] async Task should_not_pass_the_callers_activity_to_the_watchdog_loop() => (await _watchdogActivity.Task).ShouldBeNull();
    [Fact] void should_preserve_the_callers_ambient_state() => _callerAmbient.ShouldEqual("request-tenant");
    [Fact] void should_preserve_the_callers_activity() => _callerActivity.ShouldBeSame(_activity);
}
