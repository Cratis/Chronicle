// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

public class and_registration_is_cancelled_while_pending : given.a_registration_lifetime
{
    bool _registrationStillPending;
    bool _deadlineDisposed;

    async Task Because()
    {
        Connect();
        await Arrives(_entered.Task);
        _timerCreated = await Arrives(_clock.Created);
        await _cancellation.CancelAsync();
        _terminalArrived = await Arrives(_terminal.Task);
        _registrationStillPending = !_registration.Task.IsCompleted;
        _deadlineDisposed = await Arrives(_clock.Disposed);
    }

    [Fact] void should_arm_the_waiter_before_cancellation() => _timerCreated.ShouldBeTrue();
    [Fact] void should_report_a_terminal_notification() => _terminalArrived.ShouldBeTrue();
    [Fact] void should_complete_once() => _completed.ShouldEqual(1);
    [Fact] void should_not_report_an_error() => _errors.Count.ShouldEqual(0);
    [Fact] void should_leave_remote_registration_pending() => _registrationStillPending.ShouldBeTrue();
    [Fact] void should_release_the_deadline() => _deadlineDisposed.ShouldBeTrue();
    [Fact] void should_not_send_a_heartbeat() => _heartbeats.ShouldEqual(0);
    [Fact] void should_not_open_locally() => LocallyOpen.ShouldBeFalse();
    [Fact] void should_not_compensate_by_disconnecting() => Disconnects.ShouldEqual(0);
}
