// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_getting_services;

public class and_reconnecting_with_keep_alive_from_a_different_request : given.a_connection_with_ambient_state
{
    string _connectedAmbient;
    Activity _connectedActivity;
    string _callerAmbient;
    Activity _callerActivity;

    async Task Because()
    {
        _ambient.Value = "first-request";
        await _connection.Connect();
        await _keepAliveAmbient.Task.WaitAsync(TimeSpan.FromSeconds(10));
        ResetKeepAliveObservations();
        _lifecycle.Connected().Returns(_ =>
        {
            _connectedAmbient = _ambient.Value;
            _connectedActivity = Activity.Current;
            _isConnected = true;
            return Task.CompletedTask;
        });
        _isConnected = false;
        _ambient.Value = "second-request";
        _activity = new Activity("second-request").Start();
        _ = ((IChronicleServicesAccessor)_connection).Services;
        _callerAmbient = _ambient.Value;
        _callerActivity = Activity.Current;
        await _keepAliveAmbient.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_not_pass_the_new_callers_ambient_state_to_connected() => _connectedAmbient.ShouldBeNull();
    [Fact] void should_not_pass_the_new_callers_activity_to_connected() => _connectedActivity.ShouldBeNull();
    [Fact] async Task should_not_pass_either_callers_ambient_state_to_keep_alive() => (await _keepAliveAmbient.Task).ShouldBeNull();
    [Fact] async Task should_not_pass_the_new_callers_activity_to_keep_alive() => (await _keepAliveActivity.Task).ShouldBeNull();
    [Fact] void should_preserve_the_new_callers_ambient_state() => _callerAmbient.ShouldEqual("second-request");
    [Fact] void should_preserve_the_new_callers_activity() => _callerActivity.ShouldBeSame(_activity);
}
