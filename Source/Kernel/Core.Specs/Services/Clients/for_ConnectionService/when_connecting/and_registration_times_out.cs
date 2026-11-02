// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

public class and_registration_times_out : given.a_registration_lifetime
{
    bool _terminalBeforeExpiry;
    bool _registrationStillPending;

    async Task Because()
    {
        Connect();
        await Arrives(_entered.Task);
        _timerCreated = await Arrives(_clock.Created);
        _terminalBeforeExpiry = _terminal.Task.IsCompleted;
        _clock.Expire();
        _terminalArrived = await Arrives(_terminal.Task);
        _registrationStillPending = !_registration.Task.IsCompleted;
    }

    [Fact] void should_arm_the_registration_deadline() => _timerCreated.ShouldBeTrue();
    [Fact] void should_use_the_configured_response_budget() => _clock.DueTime.ShouldEqual(ResponseTimeout);
    [Fact] void should_not_terminate_before_the_explicit_deadline() => _terminalBeforeExpiry.ShouldBeFalse();
    [Fact] void should_report_a_terminal_notification() => _terminalArrived.ShouldBeTrue();
    [Fact] void should_report_a_clr_timeout_without_synthesizing_a_grpc_status() => (_errors.SingleOrDefault()?.GetType() == typeof(TimeoutException)).ShouldBeTrue();
    [Fact] void should_report_only_one_error() => _errors.Count.ShouldEqual(1);
    [Fact] void should_not_reclassify_timeout_as_cancellation() => _completed.ShouldEqual(0);
    [Fact] void should_bound_the_waiter_not_the_remote_operation() => _registrationStillPending.ShouldBeTrue();
    [Fact] void should_not_send_a_heartbeat() => _heartbeats.ShouldEqual(0);
    [Fact] void should_not_open_locally() => LocallyOpen.ShouldBeFalse();
    [Fact] void should_not_compensate_by_disconnecting_as_a_retry_reuses_the_connection_id() => Disconnects.ShouldEqual(0);
}
