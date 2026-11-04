// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

public class and_registration_is_cancelled_without_call_cancellation : given.a_registration_lifetime
{
    readonly OperationCanceledException _failure = new("Registration cancelled independently");

    async Task Because()
    {
        Connect();
        await Arrives(_entered.Task);
        _registration.SetException(_failure);
        _terminalArrived = await Arrives(_terminal.Task);
    }

    [Fact] void should_report_a_terminal_notification() => _terminalArrived.ShouldBeTrue();
    [Fact] void should_not_expose_the_internal_message() => _errors.Single().Message.Contains(_failure.Message, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_log_the_full_exception() => _logger.Entries.Any(_ => ReferenceEquals(_.Exception, _failure)).ShouldBeTrue();
    [Fact] void should_report_only_one_error() => _errors.Count.ShouldEqual(1);
    [Fact] void should_not_treat_registry_cancellation_as_call_cancellation() => _completed.ShouldEqual(0);
    [Fact] void should_not_send_a_heartbeat() => _heartbeats.ShouldEqual(0);
}
