// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookTargetPolicy.when_evaluating;

public class and_an_event_store_overrides_global_hosts : given.a_target_policy
{
    WebhookTargetValidation _global;
    WebhookTargetValidation _overridden;

    void Establish() => Configure(new()
    {
        Validation = WebhookValidationMode.Enforce,
        AllowedHosts = ["global.example"],
        EventStoreAllowedHosts = new Dictionary<string, string[]> { ["orders"] = ["store.example"] }
    });

    async Task Because()
    {
        _result = await _policy.Evaluate("https://store.example", "orders");
        _overridden = await _policy.Evaluate("https://global.example", "orders");
        _global = await _policy.Evaluate("https://global.example", "other-store");
    }

    [Fact] void should_allow_the_store_host() => _result.IsAllowed.ShouldBeTrue();
    [Fact] void should_replace_the_global_list() => _overridden.IsAllowed.ShouldBeFalse();
    [Fact] void should_fall_back_to_global_for_other_stores() => _global.IsAllowed.ShouldBeTrue();
}
