// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

public class and_registration_throws : given.a_registration_lifetime
{
    readonly InvalidOperationException _failure = new("Registration failed synchronously");

    void Establish() => _register = () => throw _failure;

    async Task Because()
    {
        Connect();
        _terminalArrived = await Arrives(_terminal.Task);
    }

    [Fact] void should_report_a_terminal_notification() => _terminalArrived.ShouldBeTrue();
    [Fact] void should_deliver_the_original_error() => ReferenceEquals(_errors.SingleOrDefault(), _failure).ShouldBeTrue();
    [Fact] void should_report_only_one_error() => _errors.Count.ShouldEqual(1);
    [Fact] void should_not_complete_normally() => _completed.ShouldEqual(0);
    [Fact] void should_not_send_a_heartbeat() => _heartbeats.ShouldEqual(0);
    [Fact] void should_not_open_locally() => LocallyOpen.ShouldBeFalse();
    [Fact] void should_not_compensate_by_disconnecting() => Disconnects.ShouldEqual(0);
}
