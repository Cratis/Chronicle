// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

public class and_cancellation_follows_registration : given.a_registration_lifetime
{
    async Task Because()
    {
        Connect();
        await Arrives(_entered.Task);
        _registration.SetResult();
        _heartbeatArrived = await Arrives(_heartbeat.Task);
        await _cancellation.CancelAsync();
        _terminalArrived = await Arrives(_terminal.Task);
        _cleanupArrived = await Arrives(_disconnected.Task);
    }

    [Fact] void should_activate_before_cancellation() => _heartbeatArrived.ShouldBeTrue();
    [Fact] void should_await_registration_before_the_first_heartbeat() => _heartbeatBeforeRegistration.ShouldBeFalse();
    [Fact] void should_report_a_terminal_notification() => _terminalArrived.ShouldBeTrue();
    [Fact] void should_complete_once() => _completed.ShouldEqual(1);
    [Fact] void should_not_report_an_error() => _errors.Count.ShouldEqual(0);
    [Fact] void should_exit_the_heartbeat_wait_promptly() => _cleanupArrived.ShouldBeTrue();
    [Fact] void should_disconnect_the_active_session_once() => Disconnects.ShouldEqual(1);
    [Fact] void should_close_the_active_local_session() => LocallyOpen.ShouldBeFalse();
    [Fact] void should_not_send_a_heartbeat_after_terminal() => _heartbeats.ShouldEqual(1);
}
