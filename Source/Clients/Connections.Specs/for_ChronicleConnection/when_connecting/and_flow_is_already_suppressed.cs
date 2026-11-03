// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_flow_is_already_suppressed : given.a_connection_with_ambient_state
{
    bool _stillSuppressed;
    bool _restored;
    string _callerAmbient;
    Activity _callerActivity;

    async Task Because()
    {
        _ambient.Value = "request-tenant";
        _activity = new Activity("request").Start();
        Task connected;
        using (ExecutionContext.SuppressFlow())
        {
            connected = _connection.Connect();
            _stillSuppressed = ExecutionContext.IsFlowSuppressed();
        }
        await connected;
        await Task.WhenAll(_keepAliveAmbient.Task, _watchdogAmbient.Task).WaitAsync(TimeSpan.FromSeconds(10));
        _restored = !ExecutionContext.IsFlowSuppressed();
        _callerAmbient = _ambient.Value;
        _callerActivity = Activity.Current;
    }

    [Fact] void should_leave_the_callers_suppression_in_place() => _stillSuppressed.ShouldBeTrue();
    [Fact] void should_restore_flow_after_the_callers_scope() => _restored.ShouldBeTrue();
    [Fact] void should_preserve_the_callers_ambient_state() => _callerAmbient.ShouldEqual("request-tenant");
    [Fact] void should_preserve_the_callers_activity() => _callerActivity.ShouldBeSame(_activity);
    [Fact] async Task should_start_keep_alive_without_the_callers_ambient_state() => (await _keepAliveAmbient.Task).ShouldBeNull();
    [Fact] async Task should_start_the_watchdog_without_the_callers_ambient_state() => (await _watchdogAmbient.Task).ShouldBeNull();
}
