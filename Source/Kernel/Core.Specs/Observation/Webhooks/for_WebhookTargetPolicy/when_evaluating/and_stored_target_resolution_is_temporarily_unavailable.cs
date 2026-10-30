// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookTargetPolicy.when_evaluating;

public class and_stored_target_resolution_is_temporarily_unavailable : given.a_target_policy
{
    WebhookTargetValidation _firstResult;
    WebhookTargetValidation _retryResult;
    readonly SocketException _dnsError = new((int)SocketError.TryAgain);

    void Establish()
    {
        Configure(new() { Validation = WebhookValidationMode.Enforce, AllowedHosts = ["receiver.example"] });
        _resolver.Resolve("receiver.example", Arg.Any<CancellationToken>()).Returns(
            _ => Task.FromException<IPAddress[]>(_dnsError),
            _ => Task.FromResult(new[] { IPAddress.Parse("8.8.8.8") }));
    }

    async Task Because()
    {
        _firstResult = await _policy.EvaluateForDelivery("https://receiver.example/hook");
        _retryResult = await _policy.EvaluateForDelivery("https://receiver.example/hook");
    }

    [Fact] void should_not_allow_an_unvalidated_target() => _firstResult.IsAllowed.ShouldBeFalse();
    [Fact] void should_explain_that_validation_can_be_retried() => _firstResult.Reason.ShouldEqual("Webhook target host resolution is temporarily unavailable. Please retry.");
    [Fact] void should_allow_the_target_after_dns_recovers() => _retryResult.IsAllowed.ShouldBeTrue();
}
