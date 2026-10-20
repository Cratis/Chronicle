// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookTargetPolicy.when_evaluating;

public class and_a_private_host_is_explicitly_approved : given.a_target_policy
{
    void Establish()
    {
        Configure(new() { Validation = WebhookValidationMode.Enforce, AllowedHosts = ["receiver.example"] });
        _resolver.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([IPAddress.Parse("10.0.0.1")]);
    }

    async Task Because() => _result = await _policy.Evaluate("https://receiver.example");

    [Fact] void should_allow_the_target() => _result.IsAllowed.ShouldBeTrue();
}
