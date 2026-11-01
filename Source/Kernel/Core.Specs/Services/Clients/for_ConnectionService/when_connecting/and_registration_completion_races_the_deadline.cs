// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

public class and_registration_completion_races_the_deadline : given.a_registration_lifetime
{
    bool _deadlineDisposed;
    bool _terminalBeforeCancellation;

    async Task Because()
    {
        Connect();
        await Arrives(_entered.Task);
        _timerCreated = await Arrives(_clock.Created);
        _registration.SetResult();
        _heartbeatArrived = await Arrives(_heartbeat.Task);
        _deadlineDisposed = await Arrives(_clock.Disposed);

        // Completion owns activation before the old deadline is offered a chance to fire.
        _clock.Expire();
        _terminalBeforeCancellation = _terminal.Task.IsCompleted;
        await _cancellation.CancelAsync();
        _terminalArrived = await Arrives(_terminal.Task);
        _cleanupArrived = await Arrives(_disconnected.Task);
    }

    [Fact] void should_arm_the_deadline_while_registration_is_pending() => _timerCreated.ShouldBeTrue();
    [Fact] void should_activate_when_registration_wins() => _heartbeatArrived.ShouldBeTrue();
    [Fact] void should_dispose_the_losing_deadline() => _deadlineDisposed.ShouldBeTrue();
    [Fact] void should_not_terminate_the_active_session_at_the_old_deadline() => _terminalBeforeCancellation.ShouldBeFalse();
    [Fact] void should_not_report_a_timeout() => _errors.Count.ShouldEqual(0);
    [Fact] void should_complete_once_after_call_cancellation() => _completed.ShouldEqual(1);
    [Fact] void should_report_a_terminal_notification() => _terminalArrived.ShouldBeTrue();
    [Fact] void should_clean_up_the_active_session() => _cleanupArrived.ShouldBeTrue();
}
