// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookTargetPolicy;

public class when_checking_managed_identity_in_off_mode : given.a_target_policy
{
    void Establish() => Configure(new()
    {
        Validation = WebhookValidationMode.Off,
        ManagedIdentity = new() { AllowedScopes = ["api://receiver/.default"], AllowSystemAssigned = true }
    });

    void Because() => _result = _policy.CheckManagedIdentity(new("", "api://receiver/.default"), "https://receiver.example");

    [Fact] void should_require_an_explicit_host_even_in_off_mode() => _result.IsAllowed.ShouldBeFalse();
    [Fact] void should_preserve_off_mode_for_other_authorization_types() => _policy.CheckAuthority("https://receiver.example").IsAllowed.ShouldBeTrue();
}
