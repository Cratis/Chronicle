// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

public class and_registration_fails_with_a_retryable_error : given.a_registration_lifetime
{
    readonly RpcException _failure = new(new Status(StatusCode.Unavailable, "Registry unavailable"));

    async Task Because()
    {
        Connect();
        await Arrives(_entered.Task);
        _registration.SetException(_failure);
        _terminalArrived = await Arrives(_terminal.Task);
    }

    [Fact] void should_report_a_terminal_notification() => _terminalArrived.ShouldBeTrue();
    [Fact] void should_preserve_the_original_retryable_error() => ReferenceEquals(_errors.SingleOrDefault(), _failure).ShouldBeTrue();
    [Fact] void should_report_only_one_error() => _errors.Count.ShouldEqual(1);
    [Fact] void should_not_convert_unavailable_to_cancellation() => _completed.ShouldEqual(0);
    [Fact] void should_not_send_a_heartbeat() => _heartbeats.ShouldEqual(0);
}
