// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_disconnecting;

public class and_the_registry_throws : given.a_registration_lifetime
{
    readonly InvalidOperationException _failure = new("Disconnect failed internally");
    bool _failureLogged;

    void Establish() => _connectedClients.OnClientDisconnected(Arg.Any<ConnectionId>(), Arg.Any<string>()).Returns(_ =>
    {
        _disconnected.TrySetResult();
        throw _failure;
    });

    async Task Because()
    {
        _registration.SetResult();
        Connect();
        _heartbeatArrived = await Arrives(_heartbeat.Task);
        await _cancellation.CancelAsync();
        _terminalArrived = await Arrives(_terminal.Task);
        _failureLogged = await Arrives(_logger.Logged.Task);
    }

    [Fact] void should_have_established_the_session() => _heartbeatArrived.ShouldBeTrue();
    [Fact] void should_have_terminated_the_stream() => _terminalArrived.ShouldBeTrue();
    [Fact] void should_complete_only_once() => _completed.ShouldEqual(1);
    [Fact] void should_not_send_an_error_after_completion() => _errors.ShouldBeEmpty();
    [Fact] void should_not_throw_out_of_connect() => _connectError.ShouldBeNull();
    [Fact] void should_log_the_disconnect_failure() => _failureLogged.ShouldBeTrue();
    [Fact] void should_log_the_full_exception_with_the_connection_id() => _logger.Entries.Any(_ => _.Level == LogLevel.Error && _.Message.Contains("the-client", StringComparison.Ordinal) && ReferenceEquals(_.Exception, _failure)).ShouldBeTrue();
    [Fact] void should_close_the_local_connection() => LocallyOpen.ShouldBeFalse();
}
