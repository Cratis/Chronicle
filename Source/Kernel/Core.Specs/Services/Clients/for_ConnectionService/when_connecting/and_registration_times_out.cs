// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

/// <summary>
/// A registry too slow to register the client within the grace must not leave the client without an answer.
/// </summary>
public class and_registration_times_out : given.a_registration_lifetime
{
    bool _servedBeforeGrace;
    bool _heartbeatArrived;
    bool _registrationStillPending;

    async Task Because()
    {
        Connect();
        await Arrives(_entered.Task);
        _servedBeforeGrace = _heartbeat.Task.IsCompleted;
        _timerCreated = await _clock.WaitForTimerDue(Grace);
        _clock.ExpireDue(Grace);
        _heartbeatArrived = await Arrives(_heartbeat.Task);
        _registrationStillPending = !_registration.Task.IsCompleted;
    }

    [Fact] void should_arm_the_grace_for_the_registration() => _timerCreated.ShouldBeTrue();
    [Fact] void should_not_serve_the_client_before_the_grace_has_passed() => _servedBeforeGrace.ShouldBeFalse();
    [Fact] void should_serve_the_client_once_the_grace_has_passed() => _heartbeatArrived.ShouldBeTrue();
    [Fact] void should_not_report_an_error() => _errors.ShouldBeEmpty();
    [Fact] void should_not_terminate_the_connection() => _terminal.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_log_that_the_registration_continues() => _logger.Entries.Any(_ => _.Message.Contains("continues in the background")).ShouldBeTrue();
    [Fact] void should_bound_the_waiter_not_the_remote_operation() => _registrationStillPending.ShouldBeTrue();
    [Fact] void should_open_locally() => LocallyOpen.ShouldBeTrue();
    [Fact] void should_not_disconnect_the_client() => Disconnects.ShouldEqual(0);
}
