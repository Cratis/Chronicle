// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookTargetPolicy.when_evaluating;

public class and_stored_target_resolution_permanently_fails : given.a_target_policy
{
    void Establish()
    {
        Configure(new() { Validation = WebhookValidationMode.Enforce, AllowedHosts = ["receiver.example"] });
        _resolver.Resolve("receiver.example", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IPAddress[]>(new SocketException((int)SocketError.HostNotFound)));
    }

    async Task Because() => _result = await _policy.EvaluateForDelivery("https://receiver.example/hook");

    [Fact] void should_reject_the_target() => _result.IsAllowed.ShouldBeFalse();
    [Fact] void should_preserve_the_safe_error() => _result.Reason.ShouldEqual("Webhook target host could not be resolved.");
}
