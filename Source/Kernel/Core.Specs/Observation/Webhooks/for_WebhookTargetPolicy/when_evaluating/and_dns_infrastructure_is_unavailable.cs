// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookTargetPolicy.when_evaluating;

public class and_dns_infrastructure_is_unavailable : given.a_target_policy
{
    [Theory]
    [InlineData(SocketError.TryAgain)]
    [InlineData(SocketError.TimedOut)]
    [InlineData(SocketError.NoRecovery)]
    [InlineData(SocketError.NetworkDown)]
    [InlineData(SocketError.NetworkUnreachable)]
    async Task should_fail_closed_with_a_retryable_explanation(SocketError error)
    {
        Configure(new() { Validation = WebhookValidationMode.Enforce, AllowedHosts = ["receiver.example"] });
        _resolver.Resolve("receiver.example", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IPAddress[]>(new SocketException((int)error)));

        var result = await _policy.EvaluateForDelivery("https://receiver.example/hook");

        result.IsAllowed.ShouldBeFalse();
        result.Reason.ShouldEqual("Webhook target host resolution is temporarily unavailable. Please retry.");
    }
}
