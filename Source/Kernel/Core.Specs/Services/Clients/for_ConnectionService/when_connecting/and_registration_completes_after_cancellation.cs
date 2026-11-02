// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

public class and_registration_completes_after_cancellation : given.a_registration_lifetime
{
    void Establish() => _register = async () =>
    {
        // Cancellation is already observable before the successful task is returned to the producer.
        await _cancellation.CancelAsync();
        _registration.SetResult();
        await _registration.Task;
    };

    async Task Because()
    {
        Connect();
        await Arrives(_entered.Task);
        _terminalArrived = await Arrives(_terminal.Task);
    }

    [Fact] void should_report_a_terminal_notification() => _terminalArrived.ShouldBeTrue();
    [Fact] void should_complete_once() => _completed.ShouldEqual(1);
    [Fact] void should_not_report_an_error() => _errors.Count.ShouldEqual(0);
    [Fact] void should_not_activate_after_observing_cancellation() => _heartbeats.ShouldEqual(0);
    [Fact] void should_not_open_locally() => LocallyOpen.ShouldBeFalse();
    [Fact] void should_not_compensate_by_disconnecting_as_a_retry_reuses_the_connection_id() => Disconnects.ShouldEqual(0);
}
