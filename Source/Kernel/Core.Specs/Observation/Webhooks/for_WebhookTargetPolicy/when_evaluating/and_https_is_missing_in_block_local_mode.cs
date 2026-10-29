// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookTargetPolicy.when_evaluating;

public class and_https_is_missing_in_block_local_mode : given.a_target_policy
{
    void Establish() => Configure(new() { Validation = WebhookValidationMode.BlockLocalAddresses, AllowedHosts = ["receiver.example"] });

    async Task Because() => _result = await _policy.Evaluate("http://receiver.example");

    [Fact] void should_allow_the_target() => _result.IsAllowed.ShouldBeTrue();
    [Fact] void should_report_the_reason() => _result.Reason.ShouldEqual("Webhook target must use HTTPS.");
}
