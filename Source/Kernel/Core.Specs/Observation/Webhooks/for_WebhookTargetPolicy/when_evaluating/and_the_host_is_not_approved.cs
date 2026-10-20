// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookTargetPolicy.when_evaluating;

public class and_the_host_is_not_approved : given.a_target_policy
{
    void Establish()
    {
        Configure(new() { Validation = WebhookValidationMode.Enforce });
    }

    async Task Because() => _result = await _policy.Evaluate("https://receiver.example");

    [Fact] void should_reject_the_target() => _result.IsAllowed.ShouldBeFalse();
}
