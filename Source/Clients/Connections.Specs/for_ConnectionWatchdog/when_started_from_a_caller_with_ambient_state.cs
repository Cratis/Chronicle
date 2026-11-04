// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Connections.for_ConnectionWatchdog;

public class when_started_from_a_caller_with_ambient_state : given.a_connection_watchdog
{
    readonly AsyncLocal<string> _ambient = new();
    Activity _activity;
    string _droppedAmbient;
    Activity _droppedActivity;
    string _reconnectAmbient;
    Activity _reconnectActivity;
    string _callerAmbient;
    bool _callerFlowSuppressed;

    void Establish()
    {
        _sessionDropped = () =>
        {
            _droppedAmbient = _ambient.Value;
            _droppedActivity = Activity.Current;
            return Task.CompletedTask;
        };
        _reconnect = () =>
        {
            _reconnectAmbient = _ambient.Value;
            _reconnectActivity = Activity.Current;
            _reconnected.TrySetResult();
            return Task.CompletedTask;
        };
    }

    async Task Because()
    {
        _ambient.Value = "request-tenant";
        _activity = new Activity("request").Start();
        _watchDog.RequestReconnect();
        _watchDog.Start();
        _callerAmbient = _ambient.Value;
        _callerFlowSuppressed = ExecutionContext.IsFlowSuppressed();
        await _reconnected.Task.WaitAsync(TimeSpan.FromSeconds(10), _time);
        await _cancellationTokenSource.CancelAsync();
    }

    void Destroy() => _activity?.Dispose();

    [Fact] void should_not_pass_the_callers_ambient_state_to_session_dropped() => _droppedAmbient.ShouldBeNull();
    [Fact] void should_not_pass_the_callers_activity_to_session_dropped() => _droppedActivity.ShouldBeNull();
    [Fact] void should_not_pass_the_callers_ambient_state_to_reconnect() => _reconnectAmbient.ShouldBeNull();
    [Fact] void should_not_pass_the_callers_activity_to_reconnect() => _reconnectActivity.ShouldBeNull();
    [Fact] void should_preserve_the_callers_ambient_state() => _callerAmbient.ShouldEqual("request-tenant");
    [Fact] void should_restore_the_callers_flow() => _callerFlowSuppressed.ShouldBeFalse();
}
