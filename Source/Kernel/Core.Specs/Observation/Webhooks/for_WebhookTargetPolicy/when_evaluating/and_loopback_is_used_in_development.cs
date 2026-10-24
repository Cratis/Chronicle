// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookTargetPolicy.when_evaluating;

public class and_loopback_is_used_in_development : given.a_target_policy
{
    void Establish()
    {
        Configure(new() { Validation = WebhookValidationMode.Enforce }, development: true);
        _resolver.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([IPAddress.Loopback]);
    }

    async Task Because() => _result = await _policy.Evaluate("http://localhost:8080/events");

    [Fact] void should_allow_the_target() => _result.IsAllowed.ShouldBeTrue();
}
