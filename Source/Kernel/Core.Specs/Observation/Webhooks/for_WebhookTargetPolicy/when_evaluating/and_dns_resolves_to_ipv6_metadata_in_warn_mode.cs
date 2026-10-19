// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookTargetPolicy.when_evaluating;

public class and_dns_resolves_to_ipv6_metadata_in_warn_mode : given.a_target_policy
{
    void Establish()
    {
        Configure(new());
        _resolver.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([IPAddress.Parse("fd00:ec2::254")]);
    }

    async Task Because() => _result = await _policy.Evaluate("https://unapproved.example");

    [Fact] void should_reject_the_target() => _result.IsAllowed.ShouldBeFalse();
    [Fact] void should_explain_the_metadata_restriction() => _result.Reason.ShouldContain("metadata");
}
