// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookTargetPolicy.when_evaluating;

public class and_a_stored_target_is_no_longer_approved : given.a_target_policy
{
    WebhookDefinition _definition;

    void Establish()
    {
        Configure(new() { Validation = WebhookValidationMode.Enforce });
        _definition = new WebhookDefinition("id", WebhookOwner.Kernel, "event-log", [], new WebhookTarget("https://previously-approved.example", WebhookAuthorization.None, new Dictionary<string, string>()), IsActive: false);
    }

    async Task Because() => _result = await _policy.Evaluate(_definition.Target.Url);

    [Fact] void should_return_an_unhealthy_reason() => _result.Reason.ShouldContain("AllowedHosts");
    [Fact] void should_block_delivery() => _result.IsAllowed.ShouldBeFalse();
    [Fact] void should_not_change_activation_state() => _definition.IsActive.ShouldBeFalse();
}
