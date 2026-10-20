// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookTargetPolicy.when_evaluating;

public class and_docker_is_used_in_development : given.a_target_policy
{
    void Establish()
    {
        Configure(new() { Validation = WebhookValidationMode.Enforce, AllowInsecureHttp = true }, development: true);
        _resolver.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([IPAddress.Parse("192.168.65.2")]);
    }

    async Task Because() => _result = await _policy.Evaluate("http://host.docker.internal:8080");

    [Fact] void should_allow_the_target() => _result.IsAllowed.ShouldBeTrue();
}
