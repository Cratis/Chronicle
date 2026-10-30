// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookTargetPolicy;

public class when_checking_managed_identity : given.a_target_policy
{
    void Establish() => Configure(new()
    {
        AllowedHosts = ["*.receiver.example"],
        EventStoreAllowedHosts = new Dictionary<string, string[]> { ["orders"] = ["orders.example"] },
        ManagedIdentity = new() { AllowedScopes = ["api://receiver/.default"], AllowedClientIds = ["Client"] }
    });

    [Fact] void should_match_scopes_ignoring_case() => _policy.CheckManagedIdentity(new("Client", "API://RECEIVER/.default"), "https://one.receiver.example").IsAllowed.ShouldBeTrue();
    [Fact] void should_match_client_ids_exactly() => _policy.CheckManagedIdentity(new("client", "api://receiver/.default"), "https://one.receiver.example").IsAllowed.ShouldBeFalse();
    [Fact] void should_not_allow_a_scope_prefix() => _policy.CheckManagedIdentity(new("Client", "api://receiver/.default/extra"), "https://one.receiver.example").IsAllowed.ShouldBeFalse();
    [Fact] void should_refuse_system_assigned_by_default() => _policy.CheckManagedIdentity(new("", "api://receiver/.default"), "https://one.receiver.example").IsAllowed.ShouldBeFalse();
    [Fact] void should_honor_the_existing_event_store_host_override() => _policy.CheckManagedIdentity(new("Client", "api://receiver/.default"), "https://orders.example", "orders").IsAllowed.ShouldBeTrue();
    [Fact] void should_not_extend_the_event_store_host_list() => _policy.CheckManagedIdentity(new("Client", "api://receiver/.default"), "https://one.receiver.example", "orders").IsAllowed.ShouldBeFalse();
    [Fact] void should_not_allow_the_bare_wildcard_domain() => _policy.CheckManagedIdentity(new("Client", "api://receiver/.default"), "https://receiver.example").IsAllowed.ShouldBeFalse();
}
