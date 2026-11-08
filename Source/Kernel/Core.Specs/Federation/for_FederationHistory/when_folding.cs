// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationHistory;

public class when_folding : Specification
{
    const string Pin = "sha256:AAAA";

    FederationHistory _history;

    void Establish() => _history = new();

    void Because()
    {
        _history.Apply("a", new FederationPeerBound("src", "https://one", Pin, "cert", "s", "Default", "t", "Default", FederationOrigin.Configuration));
        _history.Apply("a", new FederationPeerBound("src", "https://two", Pin, "cert", "s", "Default", "t", "Default", FederationOrigin.Runtime));
        _history.Apply("b", new FederationPeerBound("src", "https://b", Pin, "cert", "s", "Default", "t", "Default", FederationOrigin.Runtime));
        _history.Apply("b", new FederationPeerUnbound());
        _history.Apply("g", new FederationGrantSet(Pin, "s", ["Default"], true, [], FederationOrigin.Runtime));
        _history.Apply("g", new FederationLinkRevoked());
        _history.Apply("h", new FederationGrantSet(Pin, "s", ["Default"], false, ["x"], FederationOrigin.Runtime));
        _history.Apply("h", new FederationLinkRevoked());
        _history.Apply("h", new FederationGrantSet(Pin, "s", ["Default"], false, ["y"], FederationOrigin.Runtime));
        _history.Apply("unknown", new FederationLinkRevoked());
        _history.Apply("ignored", new object());
    }

    [Fact] void should_let_the_last_binding_event_win() => _history.Bindings.Single().Endpoint.ShouldEqual("https://two");
    [Fact] void should_remove_an_unbound_binding() => _history.HasBinding("b").ShouldBeFalse();
    [Fact] void should_remember_that_the_removed_binding_existed() => _history.HasBindingHistory("b").ShouldBeTrue();
    [Fact] void should_mark_a_revoked_grant() => _history.Grants.Single(_ => _.Id == "g").Revoked.ShouldBeTrue();
    [Fact] void should_reinstate_when_the_grant_is_set_again() => _history.Grants.Single(_ => _.Id == "h").Revoked.ShouldBeFalse();
    [Fact] void should_take_the_new_grant_content_when_reinstated() => _history.Grants.Single(_ => _.Id == "h").EventTypes.ShouldContainOnly(["y"]);
    [Fact] void should_remember_a_revocation_without_a_grant() => _history.HasGrantHistory("unknown").ShouldBeTrue();
    [Fact] void should_not_create_a_grant_from_a_revocation() => _history.HasGrant("unknown").ShouldBeFalse();
    [Fact] void should_ignore_other_events() => _history.HasBindingHistory("ignored").ShouldBeFalse();
}
