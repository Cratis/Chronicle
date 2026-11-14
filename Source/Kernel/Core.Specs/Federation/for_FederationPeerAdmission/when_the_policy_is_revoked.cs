// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPeerAdmission;

public class when_the_policy_is_revoked : given.a_peer_admission
{
    FederationAdmission _afterRevocation;
    FederationAdmission _otherSubject;
    Exception _setAgain;
    Exception _resume;

    async Task Because()
    {
        var acme = await Issued("acme", "prod");
        var globex = await Issued("globex", "prod");
        await SetPolicy("acme");
        await SetPolicy("globex");

        await _policies.Revoke("acme", "operator");
        _afterRevocation = await Admit(acme);
        _otherSubject = await Admit(globex);
        _setAgain = await Catch.Exception(() => SetPolicy("acme"));
        _resume = await Catch.Exception(() => _policies.Resume("acme", "operator"));
    }

    [Fact] void should_refuse_at_once() => _afterRevocation.Refusal.ShouldEqual(FederationAdmissionRefusal.Revoked);
    [Fact] void should_not_affect_another_subject() => _otherSubject.IsAdmitted.ShouldBeTrue();
    [Fact] void should_not_let_the_policy_be_set_again() => _setAgain.ShouldBeOfExactType<InvalidFederationPolicy>();
    [Fact] void should_not_let_the_policy_be_resumed() => _resume.ShouldBeOfExactType<InvalidFederationPolicy>();
}
