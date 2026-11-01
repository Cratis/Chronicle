// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

public class and_registration_is_cancelled_before_starting : given.a_registration_lifetime
{
    async Task Because()
    {
        await _cancellation.CancelAsync();
        Connect();
        _terminalArrived = await Arrives(_terminal.Task);
    }

    [Fact] void should_allow_subscription_to_the_terminal_replay() => _connectError.ShouldBeNull();
    [Fact] void should_report_a_terminal_notification() => _terminalArrived.ShouldBeTrue();
    [Fact] void should_complete_once() => _completed.ShouldEqual(1);
    [Fact] void should_not_report_an_error() => _errors.Count.ShouldEqual(0);
    [Fact] void should_skip_registration() => Registrations.ShouldEqual(0);
    [Fact] void should_not_send_a_heartbeat() => _heartbeats.ShouldEqual(0);
    [Fact] void should_not_open_locally() => LocallyOpen.ShouldBeFalse();
    [Fact] void should_not_compensate_by_disconnecting() => Disconnects.ShouldEqual(0);
}
