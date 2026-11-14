// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPolicies;

public class when_revoking_a_direction_of_a_policy_that_does_not_allow_it : given.a_peer_admission
{
    Exception _missing;
    Exception _revoked;

    async Task Because()
    {
        _missing = await Catch.Exception(() => _policies.RevokeDirection("nobody", FederationDirection.AcceptorInbound, "bob"));
        await _policies.Set("acme", FederationDirection.AcceptorOutbound, "store", "ns", true, [], "alice");
        await _policies.Revoke("acme", "alice");
        _revoked = await Catch.Exception(() => _policies.RevokeDirection("acme", FederationDirection.AcceptorOutbound, "bob"));
    }

    [Fact] void should_refuse_a_subject_without_a_policy() => _missing.ShouldBeOfExactType<InvalidFederationPolicy>();
    [Fact] void should_refuse_a_revoked_policy() => _revoked.ShouldBeOfExactType<InvalidFederationPolicy>();
}
