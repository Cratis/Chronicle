// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationRegistry;

public class when_revoking_and_reinstating_a_link : given.a_federation_registry
{
    IReadOnlyList<FederationGrantState> _afterRevoke;
    IReadOnlyList<FederationGrantState> _activeAfterRevoke;
    IReadOnlyList<FederationGrantState> _afterReinstate;
    IReadOnlyList<FederationGrantState> _otherLinkActive;

    async Task Because()
    {
        await _registry.SetGrant(Grant("link-1"));
        await _registry.SetGrant(Grant("link-2") with { ClientPin = _pin });
        await _registry.Revoke("link-1");
        _afterRevoke = await _registry.GetGrants();
        _activeAfterRevoke = await _registry.FindActiveGrants(CertificatePin.Parse(_otherPin));
        _otherLinkActive = await _registry.FindActiveGrants(CertificatePin.Parse(_pin));
        await _registry.SetGrant(Grant("link-1"));
        _afterReinstate = await _registry.GetGrants();
    }

    [Fact] void should_keep_the_revoked_grant_visible() => _afterRevoke.Single(_ => _.Id == "link-1").Revoked.ShouldBeTrue();
    [Fact] void should_not_find_the_revoked_grant_as_active() => _activeAfterRevoke.ShouldBeEmpty();
    [Fact] void should_leave_other_links_active() => _otherLinkActive.Single().Id.ShouldEqual("link-2");
    [Fact] void should_reinstate_when_the_grant_is_set_again() => _afterReinstate.Single(_ => _.Id == "link-1").Revoked.ShouldBeFalse();
}
