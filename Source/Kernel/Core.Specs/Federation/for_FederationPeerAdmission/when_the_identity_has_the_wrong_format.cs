// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPeerAdmission;

public class when_the_identity_has_the_wrong_format : given.a_peer_admission
{
    async Task Because()
    {
        await _authority.EnsureAuthority();
        await SetPolicy("acme");
    }

    [Theory]
    [InlineData("spiffe://example/user/acme/kernel/prod")]
    [InlineData("spiffe://example/customer/acme/node/prod")]
    [InlineData("spiffe://example/customer/acme")]
    [InlineData("spiffe://example/customer/acme/kernel/prod/extra")]
    [InlineData("spiffe://example/customer/a%2Fb/kernel/prod")]
    [InlineData("https://example/customer/acme/kernel/prod")]
    public async Task should_refuse_a_certificate_that_chains_and_was_signed_by_the_authority(string identity)
    {
        var admitted = await Admit(SignedBy(_keys.Authority, identity));
        admitted.Refusal.ShouldEqual(FederationAdmissionRefusal.WrongIdentity);
    }
}
