// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

public class and_registration_is_denied : given.a_registration_lifetime
{
    readonly RpcException _failure = new(new Status(StatusCode.PermissionDenied, "secret detail"));

    async Task Because()
    {
        Connect();
        await Arrives(_entered.Task);
        _registration.SetException(_failure);
        _terminalArrived = await Arrives(_terminal.Task);
    }

    [Fact] void should_report_a_terminal_notification() => _terminalArrived.ShouldBeTrue();
    [Fact] void should_preserve_the_non_retryable_status_code() => ((RpcException)_errors.Single()).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_send_only_generic_detail() => ((RpcException)_errors.Single()).Status.Detail.ShouldEqual("Client registration failed");
    [Fact] void should_not_attach_the_original_exception() => _errors.Single().InnerException.ShouldBeNull();
    [Fact] void should_not_attach_internal_debug_details() => ((RpcException)_errors.Single()).Status.DebugException.ShouldBeNull();
    [Fact] void should_log_the_full_exception_with_the_connection_id() => _logger.Entries.Any(_ => _.Level == LogLevel.Error && _.Message.Contains("the-client", StringComparison.Ordinal) && ReferenceEquals(_.Exception, _failure)).ShouldBeTrue();
    [Fact] void should_report_only_one_error() => _errors.Count.ShouldEqual(1);
    [Fact] void should_not_send_a_heartbeat() => _heartbeats.ShouldEqual(0);
    [Fact] void should_not_open_locally() => LocallyOpen.ShouldBeFalse();
}
