// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookTargetPolicy.when_evaluating;

public class and_a_wildcard_subdomain_is_approved : given.a_target_policy
{
    void Establish()
    {
        Configure(new() { Validation = WebhookValidationMode.Enforce, AllowedHosts = ["*.example.com"] });
    }

    async Task Because() => _result = await _policy.Evaluate("https://one.two.example.com/events");

    [Fact] void should_allow_the_target() => _result.IsAllowed.ShouldBeTrue();
}
