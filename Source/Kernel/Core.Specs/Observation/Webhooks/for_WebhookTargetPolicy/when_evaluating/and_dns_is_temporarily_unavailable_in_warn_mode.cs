// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookTargetPolicy.when_evaluating;

public class and_dns_is_temporarily_unavailable_in_warn_mode : given.a_target_policy
{
    void Establish()
    {
        Configure(new() { Validation = WebhookValidationMode.Warn });
        _resolver.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IPAddress[]>(new SocketException((int)SocketError.TryAgain)));
    }

    async Task Because() => _result = await _policy.Evaluate("https://receiver.example/hook");

    [Fact] void should_allow_with_a_warning() => _result.IsAllowed.ShouldBeTrue();
    [Fact] void should_explain_the_temporary_failure() => _result.Reason.ShouldEqual("Webhook target host resolution is temporarily unavailable. Please retry.");
}
