// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookTargetPolicy;

public class when_managed_identity_is_not_configured : given.a_target_policy
{
    void Establish() => Configure(new()
    {
        AllowedHosts = ["receiver.example"],
        ManagedIdentity = new() { AllowedClientIds = ["client"], AllowSystemAssigned = true }
    });

    void Because() => _result = _policy.CheckManagedIdentity(new("client", "api://receiver/.default"), "https://receiver.example");

    [Fact] void should_disable_managed_identity_without_allowed_scopes() => _result.IsAllowed.ShouldBeFalse();
    [Fact] void should_default_to_empty_scope_and_client_lists() => new Configuration.Webhooks().ManagedIdentity.AllowedScopes.ShouldBeEmpty();
    [Fact] void should_default_to_no_client_ids() => new Configuration.Webhooks().ManagedIdentity.AllowedClientIds.ShouldBeEmpty();
    [Fact] void should_default_to_no_system_identity() => new Configuration.Webhooks().ManagedIdentity.AllowSystemAssigned.ShouldBeFalse();
}
