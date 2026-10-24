// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookTargetPolicy.when_evaluating;

public class and_warn_is_the_default : given.a_target_policy
{
    void Establish()
    {
        Configure(new());
        _resolver.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([IPAddress.Loopback]);
    }

    async Task Because() => _result = await _policy.Evaluate("http://receiver.example");

    [Fact] void should_allow_the_target() => _result.IsAllowed.ShouldBeTrue();
    [Fact] void should_log_violations() => _logger.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == "Log").ShouldBeTrue();
}
